namespace SIVAD.Models
{
    public class Administrador : Pessoa
    {
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}