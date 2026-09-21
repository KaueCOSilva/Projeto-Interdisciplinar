using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;

public class TarefaController : Controller
{
    private List<Tarefa> lista = new List<Tarefa>();
    //https://localhost:1234/tarefa/Index
    public ActionResult Index()
    {
        Tarefa t1 = new Tarefa{IdTarefa = 1, Texto = "Task 1", Concluida = true};
        Tarefa t2 = new Tarefa{IdTarefa = 2, Texto = "Task 2", Concluida = false};
        Tarefa t3 = new Tarefa{IdTarefa = 3, Texto = "Task 3", Concluida = true};
        
        lista.Add(t1);
        lista.Add(t2);
        lista.Add(t3);

        return View(lista);
    }
[HttpGet]
public ActionResult Create()
    {
        return View();
    }
[HttpPost]
public ActionResult Create(Tarefa model)
    {
        return RedirectToAction("Index");
    }
}