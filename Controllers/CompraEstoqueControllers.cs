using Microsoft.AspNetCore.Mvc;

public class CompraEstoqueController: Controller
{
    private List<CompraEstoque> ListaCompra = new List<CompraEstoque>();

    public ActionResult Index()
    {
        return View(); //Retorna ListaCompra
    }

    


    [HttpPost]
    public ActionResult Create(CompraEstoque model)
    {
        return RedirectToAction("Index");
    }


}