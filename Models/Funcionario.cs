namespace SIVAD.Models
{
    public class Funcionario : Pessoa
    {
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}