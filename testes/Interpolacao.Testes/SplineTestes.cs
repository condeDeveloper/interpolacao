using Conde.Interpolacao;
using Xunit;

namespace Conde.Interpolacao.Testes;

public class SplineTestes
{
    [Fact]
    public void ASplinePassaPorTodosOsPontos()
    {
        var x = Nos.Iguais(11);
        var y = x.Select(Funcoes.Runge).ToArray();
        var spline = Spline.Montar(x, y);

        for (var i = 0; i < x.Length; i++) Assert.Equal(y[i], spline.Avaliar(x[i]), 1e-9);
    }

    /// <summary>
    /// As emendas sao LISAS: a inclinacao vista pela esquerda e pela direita de
    /// cada no interno bate. E a conferencia que distingue uma spline de uma
    /// curva qualquer que passa pelos pontos.
    /// </summary>
    [Fact]
    public void AsEmendasSaoLisas()
    {
        foreach (var pontos in new[] { 5, 11, 21 })
        {
            var x = Nos.Iguais(pontos);
            var spline = Spline.Montar(x, x.Select(Funcoes.Runge).ToArray());
            Assert.True(spline.MaiorSaltoNaInclinacao() < 1e-10, $"{pontos} pontos");
        }
    }

    /// <summary>
    /// E a curvatura nas pontas e zero, que e o que "natural" quer dizer. Uma
    /// implementacao errada passa por todos os pontos do mesmo jeito, e o erro
    /// so aparece aqui.
    /// </summary>
    [Fact]
    public void ACurvaturaNasPontasEhZero()
    {
        var x = Nos.Iguais(12);
        var spline = Spline.Montar(x, x.Select(Funcoes.Runge).ToArray());

        var (comeco, fim) = spline.CurvaturaNasPontas();
        Assert.Equal(0, comeco, 1e-9);
        Assert.Equal(0, fim, 1e-9);
    }

    /// <summary>
    /// Com os MESMOS nos uniformes que fazem o polinomio divergir, a spline
    /// GANHA SEMPRE, e por margens que crescem.
    /// </summary>
    [Fact]
    public void ASplineGanhaDoPolinomioEmTodoTamanho()
    {
        double razaoAnterior = 0;

        foreach (var pontos in new[] { 7, 11, 15, 21 })
        {
            var x = Nos.Iguais(pontos);
            var y = x.Select(Funcoes.Runge).ToArray();

            var spline = Erro.Maior(Funcoes.Runge, Spline.Montar(x, y).Avaliar);
            var polinomio = Erro.Maior(Funcoes.Runge,
                Lagrange.Interpolar(Erro.ComoRacionais(x, 100000), Erro.ComoRacionais(y, 100000)).Avaliar);

            Assert.True(spline < polinomio, $"com {pontos} pontos a spline deveria ganhar");

            var razao = polinomio / spline;
            Assert.True(razao > razaoAnterior, $"com {pontos} pontos a vantagem deveria crescer");
            razaoAnterior = razao;
        }

        Assert.True(razaoAnterior > 1000);
    }

    /// <summary>
    /// A convergencia da spline NAO e monotona, e isso me corrigiu.
    ///
    /// Eu escrevi que mais pontos sempre melhora, e nao e o que acontece: o erro
    /// cai de 1,3e-1 para 2,5e-3 e depois SOBE para 3,7e-3 antes de voltar a
    /// cair. A causa e a condicao de ponta: "natural" quer dizer curvatura zero
    /// nas bordas, e a funcao de Runge nao tem curvatura zero em -1 nem em 1.
    /// Esse erro de fronteira nao melhora com mais pontos no meio, e a partir de
    /// certo tamanho ele e o que domina.
    ///
    /// A spline continua ganhando do polinomio com folga. O que cai e a frase
    /// "mais pontos sempre melhora", que eu ia escrever sem medir.
    /// </summary>
    [Fact]
    public void AConvergenciaDaSplineNaoEhMonotona()
    {
        var erros = new List<double>();

        foreach (var pontos in new[] { 7, 11, 15, 19, 23 })
        {
            var x = Nos.Iguais(pontos);
            erros.Add(Erro.Maior(Funcoes.Runge, Spline.Montar(x, x.Select(Funcoes.Runge).ToArray()).Avaliar));
        }

        Assert.True(erros[^1] < erros[0] / 10, "no geral ela melhora muito");

        var subiuAlgumaVez = false;
        for (var i = 1; i < erros.Count; i++) if (erros[i] > erros[i - 1]) subiuAlgumaVez = true;
        Assert.True(subiuAlgumaVez, "e em algum passo ela piora");
    }

    [Fact]
    public void ASplineDeUmaRetaEhARetaDeVolta()
    {
        double[] x = [0, 1, 2, 3, 4];
        var y = x.Select(v => 3 * v + 1).ToArray();
        var spline = Spline.Montar(x, y);

        for (var i = 0; i <= 100; i++)
        {
            var ponto = 4.0 * i / 100;
            Assert.Equal(3 * ponto + 1, spline.Avaliar(ponto), 1e-9);
        }
    }

    [Fact]
    public void OsPedacosSaoUmAMenosQueOsPontos()
    {
        foreach (var pontos in new[] { 3, 7, 15 })
            Assert.Equal(pontos - 1, Spline.Montar(Nos.Iguais(pontos),
                         Nos.Iguais(pontos).Select(Funcoes.Runge).ToArray()).Pedacos);
    }

    [Fact]
    public void ASplinePrecisaDeTresPontos()
    {
        Assert.Throws<ArgumentException>(() => Spline.Montar([0, 1], [0, 1]));
    }

    [Fact]
    public void AsAbscissasPrecisamSerCrescentes()
    {
        Assert.Throws<ArgumentException>(() => Spline.Montar([0, 2, 1], [0, 1, 2]));
        Assert.Throws<ArgumentException>(() => Spline.Montar([0, 1, 1], [0, 1, 2]));
    }

    [Fact]
    public void AQuantidadeDeValoresPrecisaBater()
    {
        Assert.Throws<ArgumentException>(() => Spline.Montar([0, 1, 2], [0, 1]));
    }

    /// <summary>
    /// A conversao para racional arredonda, e esse arredondamento e um errinho
    /// nos valores. Ela existe porque os nos de Chebyshev sao cossenos e nao
    /// tem forma racional exata.
    /// </summary>
    [Fact]
    public void AConversaoParaRacionalArredonda()
    {
        var racionais = Erro.ComoRacionais([0.5, -0.25, 0.3333333], 1000);

        Assert.Equal(Racional.De(1, 2), racionais[0]);
        Assert.Equal(Racional.De(-1, 4), racionais[1]);
        Assert.Equal(Racional.De(333, 1000), racionais[2]);
    }

    [Fact]
    public void OErroEhAmostradoNoIntervaloInteiro()
    {
        // Uma funcao que vale zero nos nos e um no meio deles: um erro medido so
        // nos nos daria zero.
        var x = Nos.Iguais(5);
        var spline = Spline.Montar(x, new double[5]);

        Assert.Equal(0, Erro.Maior(_ => 0, spline.Avaliar), 1e-12);
        Assert.True(Erro.Maior(v => Math.Abs(v) < 0.1 ? 1 : 0, spline.Avaliar) > 0.5);
    }
}
