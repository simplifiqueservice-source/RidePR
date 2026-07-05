using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Lista pagamentos com filtros e paginacao.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PaymentQueryDto query)
    {
        var result = await _paymentService.GetPagedAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Busca um pagamento pelo identificador.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger,Driver")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _paymentService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Cria pagamento por PIX, cartao ou carteira.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentDto dto)
    {
        var result = await _paymentService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Confirma pagamento PIX e gera split.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("{id:guid}/pix/confirm")]
    public async Task<IActionResult> ConfirmPix(Guid id)
    {
        var result = await _paymentService.ConfirmPixAsync(id);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Estorna pagamento total ou parcial.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("{id:guid}/refunds")]
    public async Task<IActionResult> Refund(Guid id, RefundPaymentDto dto)
    {
        var result = await _paymentService.RefundAsync(id, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Consulta carteira de um usuario.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger,Driver")]
    [HttpGet("wallets/{userId:guid}")]
    public async Task<IActionResult> GetWallet(Guid userId)
    {
        var result = await _paymentService.GetWalletAsync(userId);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Credita saldo em carteira.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("wallets/credit")]
    public async Task<IActionResult> CreditWallet(WalletCreditDto dto)
    {
        var result = await _paymentService.CreditWalletAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }
}
