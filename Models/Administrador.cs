namespace SIVAD.Models
{
    public class Administrador : Pessoa
    {
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string usuario {get; set;} = string.Empty;
        public string senha {get; set;} = string.Empty;
    }
}