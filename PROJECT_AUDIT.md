# RidePR Backend Audit

Data: 2026-07-05
Escopo: `backend/RidePR.sln`, configuracoes backend e `docker-compose.yml`.

## Nota geral

6.8/10

O backend ja tem uma separacao inicial coerente em `Api`, `Application`, `Domain`, `Infrastructure`, `Shared` e `Tests`, usa EF Core com PostgreSQL/PostGIS, JWT, Swagger, SignalR, Redis via `IDistributedCache` e migrations versionadas. A base compila sem avisos e sem erros, mas ainda precisa endurecer seguranca, observabilidade, testes, isolamento de camadas e integracoes reais de producao antes da versao 1.0.

## Problemas encontrados

- Estrutura da solucao: havia arquivos mortos de template (`Class1.cs`) e arquivos soltos `Novo Documento de Texto.txt` dentro de projetos e pastas de dominio.
- Arquitetura em camadas: a maior parte segue Clean Architecture, mas `CompaniesController` ainda acessa `ApplicationDbContext` diretamente, atravessando Application/Infrastructure.
- Dependencias entre projetos: `Api` referencia `Application` e `Infrastructure`; `Infrastructure` referencia `Application`, `Domain` e `Shared`; `Application` referencia `Domain` e `Shared`; `Domain` permanece praticamente isolado.
- SOLID/Clean Architecture: services estao concentrando regras de negocio e orquestracao, mas alguns controllers ainda sao finos demais ou bypassam services.
- Entity Framework: modelo centralizado em `ApplicationDbContext`, com indices em campos importantes como e-mail, CPF, CNH, placas, pagamentos e localizacao por motorista.
- Migrations: migrations existem e estao versionadas; nenhuma nova migration foi necessaria porque as correcoes nao alteraram modelo persistido.
- Repositories: repositorios existem para os modulos principais; alguns metodos de leitura poderiam usar `AsNoTracking` para reduzir overhead.
- Services: services cobrem auth, usuarios, motoristas, passageiros, veiculos, viagens, pagamentos, dispatch e mapas; ainda ha risco de classes crescerem demais conforme regras aumentarem.
- Controllers: a maioria usa services e autorizacao, mas havia endpoints/hub sem autorizacao explicita.
- DTOs: DTOs de auth/usuario/senha/refresh token tinham pouca validacao de entrada.
- JWT: configurado com bearer auth, roles e Swagger, mas a chave estava assumida via null-forgiving operator e havia duplicacao de expiracao hardcoded.
- Authorization: roles existem, mas ainda falta uma politica global/fallback e checagem de propriedade do recurso por usuario autenticado.
- Swagger: configurado com esquema Bearer e XML comments quando disponivel.
- SignalR: hub de motorista existia, mas estava sem `[Authorize]` e sem suporte explicito a token por query string para WebSocket.
- Redis: usado por cache e dispatch; a lista de viagens ativas fazia read-modify-write sem protecao local contra corrida.
- RabbitMQ: esta no `docker-compose.yml`, mas nao ha codigo de publicacao/consumo no backend.
- MinIO: esta no `docker-compose.yml`, mas nao ha client/configuracao de storage no backend.
- Docker: ha `docker-compose.yml` para dependencias, mas nao ha Dockerfile/backend API no compose.
- appsettings: contem defaults locais de banco, Redis e JWT; adequado para dev, sensivel para producao se usado sem overrides.
- Logs: Serilog console/arquivo configurado; falta enriquecimento com correlation id/request id e politicas de retencao por ambiente.
- Exceptions: middleware global retorna erro generico, bom para seguranca; ainda nao diferencia erros de dominio/validacao.
- Performance: paginas limitam `PageSize` em services; queries textuais com `ToLower()` podem prejudicar indices em PostgreSQL.
- Seguranca: refresh tokens sao armazenados em texto puro; faltam rate limiting, CORS explicito, antifraude/auditoria e ownership checks por usuario.
- Codigo duplicado/morto: havia classes de template e arquivos texto duplicando trechos reais.
- Metodos/arquivos nao utilizados: `Class1.cs` e arquivos `Novo Documento de Texto.txt` eram inutilizados.
- Pacotes NuGet: nao ha falha de build por pacote; possivel revisao futura para reduzir referencias EF duplicadas entre Api/Infrastructure.
- DI: registros essenciais estao presentes e o build valida compilacao; ainda falta teste automatizado de service provider completo.
- Concorrencia: dispatch em cache distribuido ainda nao e atomicamente seguro entre multiplas instancias.
- Indices do banco: ha bons indices basicos; faltam indices compostos por status/data para dashboards, pagamentos e buscas operacionais.
- Memory leaks: nenhum vazamento obvio encontrado; `HttpClientFactory`, scoped services e hosted worker estao em padrao aceitavel.

## Problemas corrigidos

- Removidos arquivos mortos de template em `Application`, `Domain`, `Infrastructure` e `Shared`.
- Removidos arquivos `Novo Documento de Texto.txt` vazios ou duplicados do backend.
- Adicionada validacao por DataAnnotations em `RegisterDto`, `LoginDto`, `CreateUserDto`, `ChangeUserPasswordDto` e `RefreshTokenDto`.
- Adicionada validacao startup para `Jwt:Key` nula, vazia ou menor que 32 bytes.
- Adicionado suporte a JWT via `access_token` query string para `/driverHub`.
- Protegido `DriverHub` com `[Authorize(Roles = "Administrator,Driver")]`.
- Protegidos `CompaniesController`, `DriverLocationController` e `TripsController` com autorizacao por roles.
- Reduzida janela de corrida local na lista de dispatch ativo com `SemaphoreSlim` em `RedisDispatchQueue`.
- Confirmado que a solucao compila com `0` avisos e `0` erros apos as correcoes.
- Confirmado que todos os testes existentes passam.

## Problemas restantes

- `CompaniesController` ainda deve ser movido para service/repository proprio para preservar totalmente Clean Architecture.
- RabbitMQ e MinIO ainda estao apenas provisionados no compose, sem integracao real no backend.
- Nao ha Dockerfile da API nem servico backend no `docker-compose.yml`.
- `appsettings.json` contem defaults locais sensiveis; producao deve usar variaveis de ambiente/secret manager.
- Refresh tokens devem ser persistidos como hash, nao texto puro.
- Faltam ownership checks: motorista/passageiro autenticado ainda pode depender apenas de role em alguns fluxos.
- Faltam testes unitarios/integracao relevantes; existe apenas 1 teste placeholder.
- Dispatch em Redis precisa de atomicidade distribuida real para multiplas instancias, idealmente Redis primitives, Lua script ou fila/event bus.
- Falta rate limiting para login/refresh e endpoints operacionais.
- Falta politica CORS explicita para apps Flutter/web.
- Falta observabilidade estruturada: correlation id, request logging enriquecido, health checks para PostgreSQL/Redis e metricas.
- Falta revisar `ToLower()` em queries e criar indices/colations adequadas para busca case-insensitive.
- Falta tratar exceptions de dominio/validacao com status HTTP especificos.
- Falta revisar pacotes EF duplicados entre `Api` e `Infrastructure` com teste de migrations/design-time.

## Melhorias recomendadas

- Criar `CompanyService`, `ICompanyRepository` e `CompanyRepository`, removendo acesso direto ao EF em controller.
- Introduzir politica global de autorizacao e endpoints explicitamente `[AllowAnonymous]` apenas onde necessario.
- Implementar ownership checks por claim de usuario para motorista, passageiro e carteira.
- Hashear refresh tokens e armazenar somente digest com expiracao/revogacao.
- Adicionar rate limiting no login, refresh token e endpoints de dispatch.
- Adicionar health checks reais para PostgreSQL e Redis.
- Mover secrets para variaveis de ambiente e criar `appsettings.Development.json` local ignorado.
- Adicionar Dockerfile da API e incluir backend no `docker-compose.yml`.
- Integrar RabbitMQ para eventos de viagem/pagamento/dispatch quando houver consumidor definido.
- Integrar MinIO para documentos/fotos de motoristas e veiculos com URLs assinadas.
- Criar testes unitarios para services e testes de integracao para controllers/auth/EF.
- Criar migrations para indices compostos de dashboards e consultas operacionais.
- Avaliar `AsNoTracking` em queries somente leitura.
- Adicionar logs estruturados com correlation id e request context.

## Roadmap para versao 1.0

1. Endurecimento de seguranca: secrets fora do repo, refresh tokens hashed, rate limiting, CORS, ownership checks e autorizacao global.
2. Arquitetura: remover bypasses de EF nos controllers, padronizar services/repositories, adicionar testes de DI e de contratos.
3. Persistencia: revisar migrations, indices compostos, concorrencia de carteira/pagamentos e queries case-insensitive.
4. Observabilidade: health checks de dependencias, correlation id, metricas, logs estruturados e dashboard operacional.
5. Infraestrutura: Dockerfile da API, compose completo com backend, profiles dev/prod e documentacao de deploy.
6. Integracoes: RabbitMQ para eventos assincronos, MinIO para documentos e fotos, gateway real de pagamento.
7. Qualidade: ampliar cobertura de testes para auth, usuarios, motoristas, passageiros, veiculos, viagens, pagamentos, mapas e dispatch.
8. Release hardening: pipelines CI, migrations automatizadas/controladas, smoke tests e checklist operacional.

## Validacao executada

- `dotnet build backend\RidePR.sln`: compilacao com exito, `0` avisos, `0` erros.
- `dotnet test backend\RidePR.sln`: todos os testes existentes passaram, `1` aprovado, `0` falhas.
