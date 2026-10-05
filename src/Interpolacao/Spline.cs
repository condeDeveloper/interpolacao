namespace Conde.Interpolacao;

/// <summary>
/// A SPLINE CUBICA natural: um polinomio de grau tres POR INTERVALO, colados de
/// um jeito que a curva, a inclinacao e a curvatura nao dao salto nas emendas.
///
/// Ela resolve o problema de Runge por um caminho diferente do de Chebyshev. O
/// Chebyshev mantem UM polinomio e muda onde estao os nos; a spline mantem os
/// nos onde estiverem e abandona a ideia de um polinomio so. Como o grau de cada
/// pedaco nao cresce com o numero de pontos, a oscilacao nao tem como crescer.
///
/// "Natural" quer dizer que a curvatura e zero nas duas pontas. E uma escolha
/// entre varias e e a que menos inventa informacao que nao existe nos dados.
/// </summary>
public sealed class Spline
{
    private readonly double[] x;
    private readonly double[] a;
    private readonly double[] b;
    private readonly double[] c;
    private readonly double[] d;

    private Spline(double[] x, double[] a, double[] b, double[] c, double[] d)
    {
        this.x = x;
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    public int Pedacos => x.Length - 1;

    /// <summary>
    /// Monta a spline pelos pontos dados, que precisam vir com as abscissas
    /// em ordem crescente.
    /// </summary>
    public static Spline Montar(IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        ArgumentNullException.ThrowIfNull(xs);
        ArgumentNullException.ThrowIfNull(ys);
        if (xs.Count != ys.Count) throw new ArgumentException("um valor por no", nameof(ys));
        if (xs.Count < 3) throw new ArgumentException("a spline cubica precisa de ao menos tres pontos", nameof(xs));

        for (var i = 1; i < xs.Count; i++)
            if (xs[i] <= xs[i - 1]) throw new ArgumentException("as abscissas precisam ser crescentes", nameof(xs));

        var n = xs.Count - 1;
        var h = new double[n];
        for (var i = 0; i < n; i++) h[i] = xs[i + 1] - xs[i];

        // O sistema das curvaturas e TRIDIAGONAL, e e por isso que a spline e
        // barata: resolver um sistema tridiagonal custa linear, e nao cubico.
        var alfa = new double[n];
        for (var i = 1; i < n; i++)
            alfa[i] = 3 * (ys[i + 1] - ys[i]) / h[i] - 3 * (ys[i] - ys[i - 1]) / h[i - 1];

        var l = new double[n + 1];
        var mu = new double[n + 1];
        var z = new double[n + 1];
        l[0] = 1;

        for (var i = 1; i < n; i++)
        {
            l[i] = 2 * (xs[i + 1] - xs[i - 1]) - h[i - 1] * mu[i - 1];
            mu[i] = h[i] / l[i];
            z[i] = (alfa[i] - h[i - 1] * z[i - 1]) / l[i];
        }

        l[n] = 1;

        var coefA = new double[n + 1];
        var coefB = new double[n];
        var coefC = new double[n + 1];
        var coefD = new double[n];

        for (var i = 0; i <= n; i++) coefA[i] = ys[i];

        for (var j = n - 1; j >= 0; j--)
        {
            coefC[j] = z[j] - mu[j] * coefC[j + 1];
            coefB[j] = (ys[j + 1] - ys[j]) / h[j] - h[j] * (coefC[j + 1] + 2 * coefC[j]) / 3;
            coefD[j] = (coefC[j + 1] - coefC[j]) / (3 * h[j]);
        }

        return new Spline([.. xs], coefA, coefB, coefC, coefD);
    }

    public double Avaliar(double ponto)
    {
        // Fora do intervalo, a spline estende o pedaco da ponta. Extrapolacao e
        // sempre um chute, e esta e a forma mais quieta de chutar: ela nao
        // inventa curvatura nova.
        var i = 0;
        while (i < Pedacos - 1 && ponto > x[i + 1]) i++;

        var dx = ponto - x[i];
        return a[i] + b[i] * dx + c[i] * dx * dx + d[i] * dx * dx * dx;
    }

    /// <summary>
    /// A curvatura nas duas pontas, que numa spline natural tem que ser zero.
    ///
    /// E a conferencia que a comparacao de valores nao faz: uma spline que passa
    /// por todos os pontos e nao respeita a condicao de ponta nao e uma spline
    /// natural, e o erro so aparece aqui.
    /// </summary>
    public (double Comeco, double Fim) CurvaturaNasPontas()
    {
        var comeco = 2 * c[0];
        var dx = x[^1] - x[^2];
        var fim = 2 * c[Pedacos - 1] + 6 * d[Pedacos - 1] * dx;
        return (comeco, fim);
    }

    /// <summary>
    /// Se as emendas sao LISAS: a inclinacao vista pela esquerda e pela direita
    /// de cada no interno tem que bater.
    /// </summary>
    public double MaiorSaltoNaInclinacao()
    {
        double maior = 0;
        for (var i = 1; i < Pedacos; i++)
        {
            var dx = x[i] - x[i - 1];
            var pelaEsquerda = b[i - 1] + 2 * c[i - 1] * dx + 3 * d[i - 1] * dx * dx;
            maior = Math.Max(maior, Math.Abs(pelaEsquerda - b[i]));
        }
        return maior;
    }
}

/// <summary>
/// A medida de erro: a maior diferenca entre a funcao e a aproximacao, varrendo
/// o intervalo.
///
/// Ela e amostrada em muitos pontos de proposito, e nunca so nos nos: nos nos a
/// interpolacao acerta por construcao, e um erro medido so ali daria zero
/// sempre, para qualquer metodo, inclusive para um quebrado.
/// </summary>
public static class Erro
{
    public static double Maior(Func<double, double> certa, Func<double, double> aproximada,
                               double de = -1, double ate = 1, int amostras = 2000)
    {
        ArgumentNullException.ThrowIfNull(certa);
        ArgumentNullException.ThrowIfNull(aproximada);

        double maior = 0;
        for (var i = 0; i <= amostras; i++)
        {
            var x = de + (ate - de) * i / amostras;
            maior = Math.Max(maior, Math.Abs(certa(x) - aproximada(x)));
        }
        return maior;
    }

    /// <summary>
    /// Converte os nos em racionais exatos para a interpolacao simbolica.
    ///
    /// Os nos uniformes viram fracoes exatas; os de Chebyshev nao, porque sao
    /// cossenos. Por isso as medidas de ERRO rodam em ponto flutuante e as de
    /// IDENTIDADE entre as formas rodam em racional, com nos inteiros.
    /// </summary>
    public static Racional[] ComoRacionais(IReadOnlyList<double> nos, int denominador = 1000)
    {
        ArgumentNullException.ThrowIfNull(nos);
        return [.. nos.Select(n => Racional.De((long)Math.Round(n * denominador), denominador))];
    }
}
