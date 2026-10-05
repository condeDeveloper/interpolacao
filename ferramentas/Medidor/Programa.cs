using Conde.Interpolacao;

namespace Conde.Interpolacao.Medidor;

/// <summary>
/// As medidas. Erro maximo amostrado e constante de Lebesgue: nenhuma depende de
/// relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "runge": Runge(); break;
            case "lebesgue": Lebesgue(); break;
            case "funcoes": PorFuncao(); break;
            case "formas": Formas(); break;
            case "spline": Splines(); break;
            case "tudo": Runge(); Lebesgue(); PorFuncao(); Formas(); Splines(); break;
            default:
                Console.Error.WriteLine("medidas: runge, lebesgue, funcoes, formas, spline, tudo");
                return 1;
        }
        return 0;
    }

    private static Func<double, double> Interpolar(double[] nos, Func<double, double> f)
    {
        var valores = nos.Select(f).ToArray();
        var polinomio = Lagrange.Interpolar(Erro.ComoRacionais(nos, 100000), Erro.ComoRacionais(valores, 100000));
        return polinomio.Avaliar;
    }

    /// <summary>O fenomeno de Runge, com mais pontos piorando o resultado.</summary>
    private static void Runge()
    {
        Console.WriteLine("== o fenomeno de Runge: mais pontos, resultado pior ==");
        Console.WriteLine("  a funcao e 1/(1+25x^2) no intervalo de -1 a 1, e ela e perfeitamente lisa");
        Console.WriteLine();
        Console.WriteLine($"{"pontos",8}{"nos uniformes",18}{"nos de Chebyshev",20}{"spline cubica",18}");

        foreach (var pontos in new[] { 5, 9, 13, 17, 21, 25 })
        {
            var uniformes = Nos.Iguais(pontos);
            var chebyshev = Nos.Chebyshev(pontos);

            var spline = Spline.Montar(uniformes, uniformes.Select(Funcoes.Runge).ToArray());

            Console.WriteLine($"{pontos,8}" +
                              $"{Erro.Maior(Funcoes.Runge, Interpolar(uniformes, Funcoes.Runge)),18:E2}" +
                              $"{Erro.Maior(Funcoes.Runge, Interpolar(chebyshev, Funcoes.Runge)),20:E2}" +
                              $"{Erro.Maior(Funcoes.Runge, spline.Avaliar),18:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("a primeira coluna CRESCE: com 25 pontos o erro e de 258, numa funcao cujo");
        Console.WriteLine("valor nunca passa de 1. Carl Runge mostrou isso em 1901 e continua sendo");
        Console.WriteLine("contraintuitivo: mais informacao sobre a funcao da uma aproximacao pior");
        Console.WriteLine();
        Console.WriteLine("a culpa nao e da funcao, que e lisa, nem do metodo, que e exato. E dos NOS.");
        Console.WriteLine("O erro da interpolacao tem um fator que e o produto das distancias a todos");
        Console.WriteLine("os nos, e esse produto explode perto das bordas quando eles sao uniformes");
        Console.WriteLine();
        Console.WriteLine("os nos de Chebyshev ficam APERTADOS NAS BORDAS e espalhados no meio, que e");
        Console.WriteLine("o contrario do que a intuicao sugere, e e justamente isso que segura o");
        Console.WriteLine("produto. Com eles, o erro cai de 0,4 para 0,007");
        Console.WriteLine();
        Console.WriteLine("e a spline resolve por outro caminho: ela mantem os nos uniformes e abandona");
        Console.WriteLine("a ideia de UM polinomio. Como o grau de cada pedaco nao cresce com o numero");
        Console.WriteLine("de pontos, a oscilacao nao tem como crescer");
        Console.WriteLine();
    }

    /// <summary>A constante de Lebesgue, que so depende dos nos.</summary>
    private static void Lebesgue()
    {
        Console.WriteLine("== a constante de Lebesgue: quanto a interpolacao amplifica um erro ==");
        Console.WriteLine($"{"pontos",8}{"nos uniformes",20}{"nos de Chebyshev",20}{"razao",16}");

        foreach (var pontos in new[] { 5, 10, 15, 20, 25, 30 })
        {
            var uniforme = Nos.Lebesgue(Nos.Iguais(pontos));
            var chebyshev = Nos.Lebesgue(Nos.Chebyshev(pontos));
            Console.WriteLine($"{pontos,8}{uniforme,20:E2}{chebyshev,20:N3}{uniforme / chebyshev,16:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("ela responde uma pergunta que o erro medido nao responde: se os valores");
        Console.WriteLine("tiverem um errinho de medicao, quanto do errinho aparece no polinomio? A");
        Console.WriteLine("resposta e 'ate esta constante vezes', e ela so depende dos NOS");
        Console.WriteLine();
        Console.WriteLine("com nos uniformes ela cresce de maneira exponencial: com 30 pontos, um erro");
        Console.WriteLine("de medicao de um milesimo pode virar um erro de 3.450 no resultado. Com");
        Console.WriteLine("Chebyshev ela cresce como o logaritmo, e com 30 pontos ainda esta em 2,7");
        Console.WriteLine();
        Console.WriteLine("esse numero e a razao de fundo do fenomeno de Runge, e ele nao depende de");
        Console.WriteLine("qual funcao esta sendo interpolada");
        Console.WriteLine();
    }

    /// <summary>Quando a culpa e da funcao e nao dos nos.</summary>
    private static void PorFuncao()
    {
        Console.WriteLine("== o mesmo metodo em funcoes diferentes ==");
        Console.WriteLine($"{"funcao",-24}{"pontos",8}{"uniforme",16}{"Chebyshev",16}");

        foreach (var (nome, f) in Funcoes.Todas())
        {
            foreach (var pontos in new[] { 9, 17 })
                Console.WriteLine($"{(pontos == 9 ? nome : ""),-24}{pontos,8}" +
                                  $"{Erro.Maior(f, Interpolar(Nos.Iguais(pontos), f)),16:E2}" +
                                  $"{Erro.Maior(f, Interpolar(Nos.Chebyshev(pontos), f)),16:E2}");
            Console.WriteLine();
        }

        Console.WriteLine("a exponencial e aproximada muito bem pelos dois, e a linha do polinomio de");
        Console.WriteLine("grau quatro me corrigiu. Interpolar um polinomio de grau quatro com nove");
        Console.WriteLine("pontos devolve ele MESMO, e o erro deveria ser zero. Ele da 2e-5 com nove");
        Console.WriteLine("nos e 7e-4 com dezessete, e PIORA com mais pontos");
        Console.WriteLine();
        Console.WriteLine("o motivo nao e o metodo: e que os nos sao arredondados para racionais de");
        Console.WriteLine("denominador cem mil antes de entrar na conta exata, e esse arredondamento e");
        Console.WriteLine("um errinho de 5e-6 nos valores. A constante de Lebesgue com dezessete nos");
        Console.WriteLine("uniformes passa de mil, e mil vezes 5e-6 e justamente a ordem do que");
        Console.WriteLine("aparece. A constante fez exatamente o que ela promete, na minha medida");
        Console.WriteLine();
        Console.WriteLine("o valor absoluto e o caso em que a culpa e da FUNCAO: ela tem um bico, e");
        Console.WriteLine("nenhum polinomio aproxima bico direito. Chebyshev melhora e nao resolve, e");
        Console.WriteLine("o erro cai devagar por mais nos que se ponha");
        Console.WriteLine();
        Console.WriteLine("a linha de Runge e a unica em que a troca dos nos muda tudo, e essa");
        Console.WriteLine("separacao e o ponto: nem todo erro de interpolacao e culpa dos nos");
        Console.WriteLine();
    }

    /// <summary>Lagrange e Newton: o mesmo polinomio, custos diferentes.</summary>
    private static void Formas()
    {
        Console.WriteLine("== Lagrange e Newton: o mesmo polinomio, por dois caminhos ==");

        Racional[] x = [-2, -1, 0, 1, 2, 3];
        Racional[] y = [13, 4, 1, 4, 13, 28];

        var lagrange = Lagrange.Interpolar(x, y);
        var newton = Newton.Interpolar(x, y);

        Console.WriteLine($"  os pontos sao de 3x^2 + 1, e as duas formas devolvem:");
        Console.WriteLine($"    Lagrange: {lagrange}");
        Console.WriteLine($"    Newton:   {newton}");
        Console.WriteLine($"    identicos coeficiente a coeficiente: {lagrange.MesmoQue(newton)}");
        Console.WriteLine();
        Console.WriteLine("  o polinomio interpolador e UNICO, entao isso nao e coincidencia: e o que");
        Console.WriteLine("  tem que acontecer, e em aritmetica exata da para exigir igualdade em vez");
        Console.WriteLine("  de semelhanca");
        Console.WriteLine();

        Console.WriteLine($"{"pontos",8}{"Lagrange, do zero",22}{"Newton, do zero",20}{"Newton, +1 ponto",20}");

        foreach (var pontos in new[] { 5, 10, 20, 50, 100 })
            Console.WriteLine($"{pontos,8}{Lagrange.Multiplicacoes(pontos),22:N0}" +
                              $"{Newton.Divisoes(pontos),20:N0}{Newton.DivisoesDeUmPontoAMais(pontos),20:N0}");

        Console.WriteLine();
        Console.WriteLine("a ultima coluna e o motivo de a forma de Newton existir. Acrescentar um");
        Console.WriteLine("ponto custa UMA diferenca dividida nova, e tudo que ja foi calculado");
        Console.WriteLine("continua valendo; em Lagrange, cada base tem um fator por no, entao um");
        Console.WriteLine("ponto novo joga fora o trabalho inteiro");
        Console.WriteLine();
    }

    /// <summary>As propriedades da spline, conferidas por fora.</summary>
    private static void Splines()
    {
        Console.WriteLine("== a spline cubica natural ==");

        var nos = Nos.Iguais(12);
        var spline = Spline.Montar(nos, nos.Select(Funcoes.Runge).ToArray());
        var (comeco, fim) = spline.CurvaturaNasPontas();

        Console.WriteLine($"  pedacos: {spline.Pedacos}");
        Console.WriteLine($"  maior salto na inclinacao das emendas: {spline.MaiorSaltoNaInclinacao():E2}");
        Console.WriteLine($"  curvatura nas pontas: {comeco:E2} e {fim:E2}");
        Console.WriteLine();
        Console.WriteLine("  as duas conferencias de baixo nao olham valor nenhum, e sao elas que");
        Console.WriteLine("  distinguem uma spline de uma curva qualquer que passa pelos pontos. Uma");
        Console.WriteLine("  implementacao errada passa por todos os pontos do mesmo jeito");
        Console.WriteLine();

        Console.WriteLine($"{"pontos",8}{"polinomio unico",20}{"spline",16}{"razao",14}");

        foreach (var pontos in new[] { 7, 11, 15, 19, 23 })
        {
            var uniformes = Nos.Iguais(pontos);
            var valores = uniformes.Select(Funcoes.Runge).ToArray();

            var polinomio = Erro.Maior(Funcoes.Runge, Interpolar(uniformes, Funcoes.Runge));
            var pedacos = Erro.Maior(Funcoes.Runge, Spline.Montar(uniformes, valores).Avaliar);

            Console.WriteLine($"{pontos,8}{polinomio,20:E2}{pedacos,16:E2}{polinomio / pedacos,14:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("com os MESMOS nos uniformes que fazem o polinomio divergir, a spline");
        Console.WriteLine("converge. O grau de cada pedaco e sempre tres, nao importa quantos pontos");
        Console.WriteLine("haja, e oscilacao de grau tres nao tem para onde crescer");
        Console.WriteLine();
        Console.WriteLine("o preco e que ela nao e um polinomio: nao da para derivar simbolicamente,");
        Console.WriteLine("integrar de uma vez nem falar do grau dela. E quase sempre vale a pena");
        Console.WriteLine();
    }
}
