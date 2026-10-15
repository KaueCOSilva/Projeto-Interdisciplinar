const dadosBanco = JSON.parse(document.getElementById("report-data").textContent);

const meses = [
    { chave: "janeiro", nome: "Janeiro" },
    { chave: "fevereiro", nome: "Fevereiro" },
    { chave: "marco", nome: "Março" },
    { chave: "abril", nome: "Abril" },
    { chave: "maio", nome: "Maio" },
    { chave: "junho", nome: "Junho" },
    { chave: "julho", nome: "Julho" },
    { chave: "agosto", nome: "Agosto" },
    { chave: "setembro", nome: "Setembro" },
    { chave: "outubro", nome: "Outubro" },
    { chave: "novembro", nome: "Novembro" },
    { chave: "dezembro", nome: "Dezembro" }
];

const tiposDeRelatorio = {
    diarias: { titulo: "Vendas Diárias", tipoDeMovimento: "venda", agrupamento: "dia" },
    mensais: { titulo: "Vendas Mensais", tipoDeMovimento: "venda", agrupamento: "mes" },
    anuais: { titulo: "Vendas Anuais", tipoDeMovimento: "venda", agrupamento: "ano" },
    devolucoes: { titulo: "Devoluções", tipoDeMovimento: "devolucao", agrupamento: "mes" },
    caixa: { titulo: "Fluxo de Caixa", tipoDeMovimento: "todos", agrupamento: "mes" }
};

const filtroMes = document.getElementById("monthFilter");
const botoesRelatorio = document.querySelectorAll(".report-tab[data-report]");
const barras = document.getElementById("reportBars");
const eixo = document.getElementById("reportXAxis");
const tituloGrafico = document.getElementById("salesChartTitle");
const periodoSelecionado = document.getElementById("selectedPeriod");
let relatorioAtual = "mensais";

function formatarMoeda(valor) {
    return valor.toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL"
    });
}

function obterMes(data) {
    const numeroDoMes = Number(data.substring(5, 7));
    return meses[numeroDoMes - 1];
}

function obterLancamentosFiltrados(relatorio) {
    return dadosBanco.filter(function (lancamento) {
        const mesCorresponde = filtroMes.value === "todos"
            || obterMes(lancamento.data).chave === filtroMes.value;
        const tipoCorresponde = relatorio.tipoDeMovimento === "todos"
            || lancamento.tipo === relatorio.tipoDeMovimento;

        return mesCorresponde && tipoCorresponde;
    });
}

function obterValorDoLancamento(lancamento) {
    if (relatorioAtual === "caixa" && lancamento.tipo !== "venda") {
        return -lancamento.valor;
    }

    return lancamento.valor;
}

function agruparLancamentos(lancamentos, relatorio) {
    const grupos = {};

    lancamentos.forEach(function (lancamento) {
        const ano = lancamento.data.substring(0, 4);
        const mes = obterMes(lancamento.data);
        let chave;
        let rotulo;
        let ordem;

        if (relatorio.agrupamento === "dia") {
            chave = lancamento.data;
            rotulo = lancamento.data.substring(8, 10) + "/" + lancamento.data.substring(5, 7) + "/" + ano;
            ordem = lancamento.data;
        } else if (relatorio.agrupamento === "ano") {
            chave = ano;
            rotulo = ano;
            ordem = Number(ano);
        } else {
            chave = ano + "-" + mes.chave;
            rotulo = mes.nome + " " + ano;
            ordem = ano + lancamento.data.substring(5, 7);
        }

        if (!grupos[chave]) {
            grupos[chave] = { chave: chave, rotulo: rotulo, ordem: ordem, valor: 0 };
        }

        grupos[chave].valor += obterValorDoLancamento(lancamento);
    });

    return Object.values(grupos).sort(function (a, b) {
        if (typeof a.ordem === "string") {
            return a.ordem.localeCompare(b.ordem);
        }

        return a.ordem - b.ordem;
    });
}

function desenharGrafico(grupos) {
    barras.replaceChildren();
    eixo.replaceChildren();

    if (grupos.length === 0) {
        const mensagem = document.createElement("div");
        mensagem.className = "report-empty-message";
        mensagem.textContent = "Não há lançamentos do banco para este filtro.";
        barras.appendChild(mensagem);
        desenharEixo(0);
        return;
    }

    let maiorValor = 0;
    grupos.forEach(function (grupo) {
        maiorValor = Math.max(maiorValor, Math.abs(grupo.valor));
    });

    grupos.forEach(function (grupo) {
        const linha = document.createElement("div");
        linha.className = "bar-row";

        const rotulo = document.createElement("span");
        rotulo.className = "month-label";
        rotulo.textContent = grupo.rotulo;

        const recipienteBarra = document.createElement("div");
        recipienteBarra.className = "bar-container";

        const barra = document.createElement("div");
        barra.className = "bar";
        barra.style.width = maiorValor === 0
            ? "0%"
            : Math.max(4, Math.round(Math.abs(grupo.valor) / maiorValor * 100)) + "%";
        barra.title = grupo.rotulo + ": " + formatarMoeda(grupo.valor);

        if (relatorioAtual === "devolucoes") {
            barra.style.backgroundColor = "#8b5cf6";
        } else if (relatorioAtual === "caixa" && grupo.valor < 0) {
            barra.style.backgroundColor = "#dc4c4c";
        }

        recipienteBarra.appendChild(barra);
        linha.appendChild(rotulo);
        linha.appendChild(recipienteBarra);
        barras.appendChild(linha);
    });

    desenharEixo(maiorValor);
}

function desenharEixo(maiorValor) {
    const valorMaximo = Math.ceil(maiorValor / 100) * 100 || 100;

    for (let ponto = 0; ponto <= 4; ponto++) {
        const rotulo = document.createElement("span");
        const valor = Math.round(valorMaximo * ponto / 4);
        rotulo.textContent = "R$ " + valor.toLocaleString("pt-BR");
        eixo.appendChild(rotulo);
    }
}

function atualizarIndicadores(lancamentos, grupos, relatorio) {
    let total = 0;
    let vendas = 0;
    let saidas = 0;

    lancamentos.forEach(function (lancamento) {
        if (lancamento.tipo === "venda") {
            vendas += lancamento.valor;
        } else {
            saidas += lancamento.valor;
        }
        total += obterValorDoLancamento(lancamento);
    });

    const quantidade = lancamentos.length;
    const media = quantidade === 0 ? 0 : total / quantidade;
    const nomeMes = filtroMes.value === "todos"
        ? "Todos os meses"
        : filtroMes.options[filtroMes.selectedIndex].text;

    document.getElementById("primaryMetricLabel").textContent = relatorio.titulo;
    document.getElementById("primaryMetricValue").textContent = formatarMoeda(total);
    document.getElementById("primaryMetricDescription").textContent = "Dados consultados no SQL Server";

    const rotuloSecundario = relatorio.tipoDeMovimento === "devolucao"
        ? "Valor devolvido"
        : relatorio.tipoDeMovimento === "todos" ? "Entradas de vendas" : "Receita de vendas";
    const valorSecundario = relatorio.tipoDeMovimento === "devolucao" ? total : vendas;
    document.getElementById("secondMetricLabel").textContent = rotuloSecundario;
    document.getElementById("secondMetricValue").textContent = formatarMoeda(valorSecundario);
    document.getElementById("secondMetricDescription").textContent = "Soma no período selecionado";

    if (relatorioAtual === "caixa") {
        document.getElementById("thirdMetricLabel").textContent = "Saídas no período";
        document.getElementById("thirdMetricValue").textContent = formatarMoeda(saidas);
        document.getElementById("thirdMetricDescription").textContent = "Compras de estoque registradas";
    } else {
        document.getElementById("thirdMetricLabel").textContent = "Registros no filtro";
        document.getElementById("thirdMetricValue").textContent = quantidade.toString();
        document.getElementById("thirdMetricDescription").textContent = "Lançamentos no SQL Server";
    }

    document.getElementById("fourthMetricLabel").textContent = "Mês selecionado";
    document.getElementById("fourthMetricValue").textContent = nomeMes;
    document.getElementById("fourthMetricDescription").textContent = "Filtro combinado com a aba";

    document.getElementById("selectedPeriod").textContent = nomeMes;
    document.getElementById("transactionCount").textContent = quantidade + (quantidade === 1 ? " registro" : " registros");
    document.getElementById("averageTicketLabel").textContent = relatorio.tipoDeMovimento === "venda"
        ? "Média por venda"
        : "Média por registro";
    document.getElementById("averageTicket").textContent = formatarMoeda(media);

    document.getElementById("bestGroupLabel").textContent = "Maior valor no gráfico";
    if (grupos.length === 0) {
        document.getElementById("bestGroupValue").textContent = "Sem dados";
        document.getElementById("bestGroupDescription").textContent = "Nenhum lançamento neste filtro";
    } else {
        let maiorGrupo = grupos[0];
        grupos.forEach(function (grupo) {
            if (Math.abs(grupo.valor) > Math.abs(maiorGrupo.valor)) {
                maiorGrupo = grupo;
            }
        });
        document.getElementById("bestGroupValue").textContent = maiorGrupo.rotulo;
        document.getElementById("bestGroupDescription").textContent = formatarMoeda(maiorGrupo.valor);
    }

    document.getElementById("reportTypeLabel").textContent = "Relatório selecionado";
    document.getElementById("reportTypeValue").textContent = relatorio.titulo;
    document.getElementById("reportTypeDescription").textContent = "Período: " + nomeMes;

    document.getElementById("recordsLabel").textContent = "Origem dos dados";
    document.getElementById("recordsValue").textContent = "SQL Server";
    document.getElementById("recordsDescription").textContent = relatorioAtual === "devolucoes"
        ? "Não há tabela de devoluções no schema atual"
        : "Movimentos carregados do banco de dados";
}

function aplicarFiltros() {
    const relatorio = tiposDeRelatorio[relatorioAtual];
    const lancamentos = obterLancamentosFiltrados(relatorio);
    const grupos = agruparLancamentos(lancamentos, relatorio);
    const nomeMes = filtroMes.value === "todos"
        ? "Todos os meses"
        : filtroMes.options[filtroMes.selectedIndex].text;

    botoesRelatorio.forEach(function (botao) {
        const estaAtivo = botao.dataset.report === relatorioAtual;
        botao.classList.toggle("active", estaAtivo);
        botao.setAttribute("aria-pressed", estaAtivo.toString());
    });

    tituloGrafico.textContent = relatorio.titulo + " — " + nomeMes;
    desenharGrafico(grupos);
    atualizarIndicadores(lancamentos, grupos, relatorio);
}

botoesRelatorio.forEach(function (botao) {
    botao.addEventListener("click", function () {
        relatorioAtual = botao.dataset.report;
        aplicarFiltros();
    });
});

filtroMes.addEventListener("change", aplicarFiltros);
aplicarFiltros();
