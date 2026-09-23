using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    public class CalculoTotalController : Controller
    {
        private readonly TotalPedidoStrategy _totalPedidoStrategy;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CalculoTotalController(
            TotalPedidoStrategy totalPedidoStrategy,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _totalPedidoStrategy = totalPedidoStrategy;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        // Acessível via: /CalculoTotal/CalcularPedido
        [HttpGet]
        public IActionResult CalcularPedido()
        {
            // Criando um pedido simulado para teste
            var pedido = new Pedido
            {
                Codigo = 1,
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Qtd = 2, PrecoTotal = 50.00m }, // 100.00
                    new ItemPedido { Qtd = 1, PrecoTotal = 35.50m }  // 35.50
                }
            };

            float total = _totalPedidoStrategy.CalcularTotal(pedido);

            // Passa o valor 'total' para a View CalcularPedido.cshtml
            return View(total);
        }

        // Acessível via: /CalculoTotal/CalcularCompraEstoque
        [HttpGet]
        public IActionResult CalcularCompraEstoque()
        {
            // Criando uma compra de estoque simulada para teste
            var compra = new CompraEstoque
            {
                Numero = 1,
                Itens = new List<ItemCompraEstoque>
                {
                    new ItemCompraEstoque { Quantidade = 10, Valor = 12.00m }, // 120.00
                    new ItemCompraEstoque { Quantidade = 5,  Valor = 30.00m }  // 150.00
                }
            };

            float total = _totalCompraEstoqueStrategy.CalcularTotal(compra);

            // Passa o valor 'total' para a View CalcularCompraEstoque.cshtml
            return View(total);
        }
    }
}