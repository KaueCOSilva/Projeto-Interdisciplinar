SELECT p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque,
                   p.categoria_codigo, c.nome AS categoria_nome, p.cor,
                   COALESCE(ultimaCompra.fornecedor_nome, '') AS fornecedor_nome,
                   p.ativo
            FROM Produtos p
            INNER JOIN Categorias c ON c.codigo = p.categoria_codigo
            OUTER APPLY
            (
                SELECT TOP (1) ce.fornecedor_nome
                FROM Itens_CompraEstoque ic
                INNER JOIN CompraEstoque ce ON ce.numero = ic.compra_numero AND ce.status = 1
                WHERE ic.produto_codigo = p.codigo
                ORDER BY ce.data_compra DESC, ce.numero DESC
            ) ultimaCompra
            -- Funcionário recebe somente produtos ativos com saldo positivo;
            -- o administrador pode consultar inativos quando precisa editar histórico.
            WHERE (@incluirInativos = 1 OR p.ativo = 1)
              AND (@somenteComEstoque = 0 OR p.estoque > 0)
            ORDER BY p.nome, p.codigo;
            """;

            