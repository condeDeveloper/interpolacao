namespace Conde.Interpolacao;

/// <summary>
/// A forma de LAGRANGE: o polinomio escrito como uma soma de bases, cada uma
/// valendo um no proprio no e zero em todos os outros.
///
/// E a forma mais facil de entender e a pior de usar. Acrescentar um ponto
/// obriga a refazer TODAS as bases, porque cada uma tem um fator por no.
/// </summary>
public static class Lagrange
{
    /// <summary>
    /// A base de Lagrange do indice dado: vale um em x[i] e zero em todo outro
    /// no.
    /// </summary>
    public static Polinomio Base(IReadOnlyList<Racional> x, int indice)
    {
        ArgumentNullException.ThrowIfNull(x);

        var saida = Polinomio.Constante(Racional.Um);
        for (var j = 0; j < x.Count; j++)
        {
            if (j == indice) continue;
            saida *= Polinomio.Fator(x[j]) * (Racional.Um / (x[indice] - x[j]));
        }
        return saida;
    }

    /// <summary>O polinomio que passa pelos pontos dados.</summary>
    public static Polinomio Interpolar(IReadOnlyList<Racional> x, IReadOnlyList<Racional> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Count != y.Count) throw new ArgumentException("um valor por no", nameof(y));
        if (x.Distinct().Count() != x.Count) throw new ArgumentException("nos repetidos", nameof(x));

        var saida = Polinomio.Zero;
        for (var i = 0; i < x.Count; i++) saida += Base(x, i) * y[i];
        return saida;
    }

    /// <summary>Quantas multiplicacoes a forma gasta para montar o polinomio.</summary>
    public static long Multiplicacoes(int pontos) => (long)pontos * pontos;
}

/// <summary>
/// A forma de NEWTON, por diferencas divididas: o mesmo polinomio escrito como
/// uma soma em que cada termo acrescenta um no.
///
/// A vantagem e exatamente essa: acrescentar um ponto custa UMA diferenca
/// dividida nova, e tudo que ja foi calculado continua valendo. Em Lagrange,
/// acrescentar um ponto joga fora o trabalho inteiro.
///
/// O resultado e o MESMO polinomio, porque ele e unico. Um teste confere isso em
/// aritmetica exata, coeficiente a coeficiente.
/// </summary>
public static class Newton
{
    /// <summary>
    /// A tabela de diferencas divididas. A primeira coluna sao os valores, e
    /// cada coluna seguinte e a diferenca da anterior dividida pelo intervalo.
    /// </summary>
    public static Racional[] Diferencas(IReadOnlyList<Racional> x, IReadOnlyList<Racional> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Count != y.Count) throw new ArgumentException("um valor por no", nameof(y));

        var n = x.Count;
        var tabela = y.ToArray();
        var saida = new Racional[n];
        saida[0] = tabela[0];

        for (var nivel = 1; nivel < n; nivel++)
        {
            for (var i = 0; i < n - nivel; i++)
                tabela[i] = (tabela[i + 1] - tabela[i]) / (x[i + nivel] - x[i]);
            saida[nivel] = tabela[0];
        }

        return saida;
    }

    public static Polinomio Interpolar(IReadOnlyList<Racional> x, IReadOnlyList<Racional> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        var coeficientes = Diferencas(x, y);

        var saida = Polinomio.Zero;
        var produto = Polinomio.Constante(Racional.Um);

        for (var i = 0; i < coeficientes.Length; i++)
        {
            saida += produto * coeficientes[i];
            produto *= Polinomio.Fator(x[i]);
        }

        return saida;
    }

    /// <summary>
    /// Acrescenta UM ponto a uma tabela de diferencas ja pronta.
    ///
    /// E o que a forma de Newton compra, e o custo e linear no numero de pontos
    /// em vez de quadratico.
    /// </summary>
    public static Racional[] Acrescentar(IReadOnlyList<Racional> x, IReadOnlyList<Racional> y,
                                         IReadOnlyList<Racional> diferencas)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        ArgumentNullException.ThrowIfNull(diferencas);

        var n = x.Count;
        var saida = new Racional[n];
        for (var i = 0; i < diferencas.Count; i++) saida[i] = diferencas[i];

        // A diferenca nova sai do valor do ponto novo menos o que o polinomio
        // antigo ja preve ali, dividido pelo produto dos intervalos.
        var antigo = Polinomio.Zero;
        var produto = Polinomio.Constante(Racional.Um);
        for (var i = 0; i < n - 1; i++)
        {
            antigo += produto * saida[i];
            produto *= Polinomio.Fator(x[i]);
        }

        saida[n - 1] = (y[n - 1] - antigo.Avaliar(x[n - 1])) / produto.Avaliar(x[n - 1]);
        return saida;
    }

    /// <summary>Quantas divisoes a tabela de diferencas gasta.</summary>
    public static long Divisoes(int pontos) => (long)pontos * (pontos - 1) / 2;

    /// <summary>E quantas um ponto a mais custa, que e o motivo de ela existir.</summary>
    public static long DivisoesDeUmPontoAMais(int pontos) => pontos;
}
