using Conde.Interpolacao;
using Xunit;

namespace Conde.Interpolacao.Testes;

public class RungeTestes
{
    private static Func<double, double> Interpolar(double[] nos, Func<double, double> f)
    {
        var valores = nos.Select(f).ToArray();
        return Lagrange.Interpolar(Erro.ComoRacionais(nos, 100000), Erro.ComoRacionais(valores, 100000)).Avaliar;
    }

    /// <summary>
    /// O fenomeno de Runge: com nos uniformes, MAIS PONTOS da um resultado PIOR.
    /// </summary>
    [Fact]
    public void ComNosUniformesMaisPontosPioraOResultado()
    {
        double anterior = 0;
        foreach (var pontos in new[] { 9, 13, 17, 21, 25 })
        {
            var erro = Erro.Maior(Funcoes.Runge, Interpolar(Nos.Iguais(pontos), Funcoes.Runge));
            Assert.True(erro > anterior, $"com {pontos} pontos o erro deveria ter crescido");
            anterior = erro;
        }

        Assert.True(anterior > 100, $"com 25 pontos o erro deveria passar de 100, deu {anterior:N1}");
    }

    /// <summary>E com os nos de Chebyshev, mais pontos melhora.</summary>
    [Fact]
    public void ComChebyshevMaisPontosMelhora()
    {
        double anterior = double.MaxValue;
        foreach (var pontos in new[] { 9, 13, 17, 21, 25 })
        {
            var erro = Erro.Maior(Funcoes.Runge, Interpolar(Nos.Chebyshev(pontos), Funcoes.Runge));
            Assert.True(erro < anterior, $"com {pontos} pontos o erro deveria ter caido");
            anterior = erro;
        }

        Assert.True(anterior < 0.01, $"com 25 pontos o erro deveria ser minusculo, deu {anterior:E2}");
    }

    /// <summary>
    /// A funcao de Runge nao tem nada de errado: ela e lisa e o valor dela nunca
    /// passa de um. O erro de 258 nao vem dela.
    /// </summary>
    [Fact]
    public void AFuncaoDeRungeEhBemComportada()
    {
        for (var i = 0; i <= 1000; i++)
        {
            var x = -1 + 2.0 * i / 1000;
            Assert.InRange(Funcoes.Runge(x), 0, 1);
        }
    }

    [Fact]
    public void OsNosUniformesSaoUniformes()
    {
        var nos = Nos.Iguais(11, -1, 1);
        Assert.Equal(11, nos.Length);
        Assert.Equal(-1, nos[0], 1e-12);
        Assert.Equal(1, nos[^1], 1e-12);

        var passo = nos[1] - nos[0];
        for (var i = 1; i < nos.Length; i++) Assert.Equal(passo, nos[i] - nos[i - 1], 1e-12);
    }

    /// <summary>
    /// Os nos de Chebyshev ficam APERTADOS NAS BORDAS, que e o contrario do que
    /// a intuicao sugere, e e justamente isso que faz a conta funcionar.
    /// </summary>
    [Fact]
    public void OsNosDeChebyshevSaoApertadosNasBordas()
    {
        var nos = Nos.Chebyshev(11, -1, 1);
        Assert.Equal(11, nos.Length);

        var naBorda = nos[1] - nos[0];
        var noMeio = nos[6] - nos[5];
        Assert.True(naBorda < noMeio / 2, $"borda {naBorda:N3} deveria ser bem menor que meio {noMeio:N3}");
    }

    [Fact]
    public void OsNosSaemOrdenados()
    {
        foreach (var quantos in new[] { 3, 7, 15 })
        {
            var chebyshev = Nos.Chebyshev(quantos);
            for (var i = 1; i < quantos; i++) Assert.True(chebyshev[i] > chebyshev[i - 1]);
        }
    }

    /// <summary>
    /// A constante de Lebesgue cresce de maneira explosiva com nos uniformes e
    /// devagar com Chebyshev. Ela so depende dos NOS, nao da funcao.
    /// </summary>
    [Fact]
    public void ALebesgueExplodeComNosUniformesENaoComChebyshev()
    {
        var uniforme = Nos.Lebesgue(Nos.Iguais(25));
        var chebyshev = Nos.Lebesgue(Nos.Chebyshev(25));

        Assert.True(uniforme > 10000, $"a uniforme deu {uniforme:E2}");
        Assert.True(chebyshev < 4, $"a de Chebyshev deu {chebyshev:N3}");
    }

    [Fact]
    public void ALebesgueDeUmNoSoVale1()
    {
        Assert.Equal(1, Nos.Lebesgue([0.0]), 1e-9);
    }

    [Fact]
    public void ALebesgueNuncaEhMenorQueUm()
    {
        foreach (var quantos in new[] { 2, 5, 10, 20 })
        {
            Assert.True(Nos.Lebesgue(Nos.Iguais(quantos)) >= 1);
            Assert.True(Nos.Lebesgue(Nos.Chebyshev(quantos)) >= 1);
        }
    }

    /// <summary>
    /// O valor absoluto e o caso em que a culpa e da FUNCAO: nem Chebyshev
    /// resolve, porque nenhum polinomio aproxima bico direito.
    /// </summary>
    [Fact]
    public void ComBicoNemChebyshevResolve()
    {
        var comNove = Erro.Maior(Funcoes.Bico, Interpolar(Nos.Chebyshev(9), Funcoes.Bico));
        var comDezessete = Erro.Maior(Funcoes.Bico, Interpolar(Nos.Chebyshev(17), Funcoes.Bico));

        Assert.True(comDezessete < comNove, "deveria melhorar");
        Assert.True(comDezessete > 0.01, "e nao deveria resolver");
    }

    /// <summary>
    /// A exponencial e aproximada muito bem pelos dois, e e a linha que mostra
    /// que nem toda funcao tem problema.
    /// </summary>
    [Fact]
    public void AExponencialEhFacilParaOsDois()
    {
        Assert.True(Erro.Maior(Funcoes.Exponencial, Interpolar(Nos.Iguais(9), Funcoes.Exponencial)) < 1e-4);
        Assert.True(Erro.Maior(Funcoes.Exponencial, Interpolar(Nos.Chebyshev(9), Funcoes.Exponencial)) < 1e-4);
    }

    /// <summary>
    /// O erro e amostrado no intervalo inteiro, e nunca so nos nos. Nos nos a
    /// interpolacao acerta por construcao, e um erro medido so ali daria zero
    /// para qualquer metodo, inclusive para um quebrado.
    /// </summary>
    [Fact]
    public void NosNosOErroEhSempreQuaseZero()
    {
        var nos = Nos.Iguais(15);
        var polinomio = Interpolar(nos, Funcoes.Runge);

        foreach (var no in nos) Assert.Equal(Funcoes.Runge(no), polinomio(no), 1e-3);

        // E no intervalo inteiro ele e enorme.
        Assert.True(Erro.Maior(Funcoes.Runge, polinomio) > 1);
    }
}
