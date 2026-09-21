using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.VisualBasic.FileIO;

public class Tarefa
{
    //private string texto;

    public string Texto { get; set; }
    
    public int IdTarefa { get; set; }

    public bool Concluida { get; set; }

    // propriedade
    // public string Texto
    //{
        //get {return texto;}
        //set {texto = value;}
    //}
    // var t = new Tarefa();
    // t.Texto = "Task 1";
    // Console.Write(t.Texto);
}