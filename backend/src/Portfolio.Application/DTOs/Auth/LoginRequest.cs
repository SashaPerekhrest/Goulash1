using System.ComponentModel.DataAnnotations;

namespace Portfolio.Application.DTOs.Auth;

public sealed record LoginRequest(
    [Required]
    [MaxLength(100)]
    string Login,

    [Required]
    string Password);
