namespace Conde.Interpolacao;

/// <summary>
/// Onde colocar os nos, que e a decisao que define se a interpolacao funciona.
///
/// Quase todo material trata a escolha dos nos como detalhe e o grau do
/// polinomio como o que importa. E o contrario: com nos igualmente espacados,
/// aumentar o grau PIORA o resultado; com nos de Chebyshev, melhora. A mesma
/// funcao, o mesmo numero de pontos, o mesmo algoritmo.
/// </summary>
public static class Nos
{
    /// <summary>
    /// Nos IGUALMENTE ESPACADOS no intervalo dado.
    ///
    /// E a escolha obvia e a que produz o fenomeno de Runge. O problema nao e a
    /// funcao nem o metodo: e que o polinomio que passa por pontos igualmente
    /// espacados oscila violentamente perto das bordas, e a oscilacao cresce com
    /// o grau.
    /// </summary>
    public static double[] Iguais(int quantos, double de = -1, double ate = 1)
    {
        if (quantos < 1) throw new ArgumentOutOfRangeException(nameof(quantos));
        if (quantos == 1) return [(de + ate) / 2];

        var saida = new double[quantos];
        for (var i = 0; i < quantos; i++) saida[i] = de + (ate - de) * i / (quantos - 1);
        return saida;
    }

    /// <summary>
    /// Nos de CHEBYSHEV: as projecoes de pontos igualmente espacados num
    /// semicirculo.
    ///
    /// Eles ficam APERTADOS NAS BORDAS e espalhados no meio, que e exatamente o
    /// contrario do que a intuicao sugere. A razao e que o erro da interpolacao
    /// tem um fator que e o produto das distancias a todos os nos, e esse produto
    /// explode perto das bordas quando os nos sao uniformes. Apertar os nos ali
    /// segura o produto.
    ///
    /// Com eles, o maior valor desse produto fica o MENOR possivel, e nenhuma
    /// outra escolha de nos consegue menos.
    /// </summary>
    public static double[] Chebyshev(int quantos, double de = -1, double ate = 1)
    {
        if (quantos < 1) throw new ArgumentOutOfRangeException(nameof(quantos));

        var saida = new double[quantos];
        for (var i = 0; i < quantos; i++)
        {
            var angulo = Math.PI * (2.0 * i + 1) / (2.0 * quantos);
            var padrao = Math.Cos(angulo);
            saida[i] = (de + ate) / 2 + (ate - de) / 2 * padrao;
        }

        Array.Sort(saida);
        return saida;
    }

    /// <summary>
    /// A CONSTANTE DE LEBESGUE: o quanto a interpolacao pode amplificar um erro
    /// nos valores.
    ///
    /// Ela responde uma pergunta que o erro medido nao responde: se os valores
    /// tiverem um errinho de medicao, quanto do errinho aparece no polinomio?
    /// A resposta e "ate esta constante vezes", e ela so depende dos NOS, nao da
    /// funcao.
    ///
    /// Para nos uniformes ela cresce de maneira exponencial; para Chebyshev, de
    /// maneira logaritmica. A medida mostra os dois.
    /// </summary>
    public static double Lebesgue(IReadOnlyList<double> nos, int amostras = 2000)
    {
        ArgumentNullException.ThrowIfNull(nos);

        var de = nos.Min();
        var ate = nos.Max();
        double maior = 0;

        for (var a = 0; a <= amostras; a++)
        {
            var x = de + (ate - de) * a / amostras;
            double soma = 0;

            for (var i = 0; i < nos.Count; i++)
            {
                double produto = 1;
                for (var j = 0; j < nos.Count; j++)
                {
                    if (j == i) continue;
                    produto *= (x - nos[j]) / (nos[i] - nos[j]);
                }
                soma += Math.Abs(produto);
            }

            maior = Math.Max(maior, soma);
        }

        return maior;
    }

    /// <summary>As duas escolhas, com nome, para as varreduras.</summary>
    public static IEnumerable<(string Nome, Func<int, double, double, double[]> Montar)> Todas()
    {
        yield return ("igualmente espacados", Iguais);
        yield return ("Chebyshev", Chebyshev);
    }
}

/// <summary>As funcoes de teste.</summary>
public static class Funcoes
{
    /// <summary>
    /// A funcao de RUNGE: 1 / (1 + 25x^2).
    ///
    /// Carl Runge usou ela em 1901 para mostrar que interpolar com mais pontos
    /// pode piorar o resultado, o que era contraintuitivo na epoca e continua
    /// sendo. Ela e perfeitamente lisa, sem bico, sem salto e sem nada estranho:
    /// o problema esta na escolha dos nos.
    /// </summary>
    public static double Runge(double x) => 1.0 / (1 + 25 * x * x);

    /// <summary>
    /// O valor absoluto: uma funcao com BICO.
    ///
    /// Nenhum polinomio aproxima bem um bico, e o erro cai devagar por mais nos
    /// que se ponha. E o caso em que a culpa e da funcao, e nao da escolha dos
    /// nos, e ele existe aqui para separar os dois.
    /// </summary>
    public static double Bico(double x) => Math.Abs(x);

    /// <summary>A exponencial, que polinomio aproxima maravilhosamente bem.</summary>
    public static double Exponencial(double x) => Math.Exp(x);

    /// <summary>Um polinomio de grau quatro, que a interpolacao acerta na mosca.</summary>
    public static double Quartica(double x) => x * x * x * x - 3 * x * x + 1;

    public static IEnumerable<(string Nome, Func<double, double> F)> Todas()
    {
        yield return ("Runge, 1/(1+25x^2)", Runge);
        yield return ("valor absoluto", Bico);
        yield return ("exponencial", Exponencial);
        yield return ("x^4 - 3x^2 + 1", Quartica);
    }
}
