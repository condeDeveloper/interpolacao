using Conde.Interpolacao;
using Xunit;

namespace Conde.Interpolacao.Testes;

/// <summary>
/// O polinomio interpolador e UNICO, entao as duas formas tem que devolver
/// exatamente o mesmo objeto. Em aritmetica exata da para exigir igualdade em
/// vez de semelhanca.
/// </summary>
public class FormasTestes
{
    [Fact]
    public void ARacionalReduzSozinha()
    {
        Assert.Equal(Racional.De(1, 2), Racional.De(2, 4));
        Assert.Equal(Racional.De(-1, 2), Racional.De(1, -2));
        Assert.Equal("1/2", Racional.De(2, 4).ToString());
    }

    [Fact]
    public void ARacionalPadraoVale0Sobre1()
    {
        var padrao = default(Racional);
        Assert.True(padrao.EhZero);
        Assert.Equal(0, padrao.Valor(), 1e-12);
    }

    [Fact]
    public void OPolinomioCortaOsZerosDoFim()
    {
        var polinomio = new Polinomio([Racional.Um, Racional.Zero, Racional.Zero]);
        Assert.Equal(0, polinomio.Grau);
        Assert.Single(polinomio.Coeficientes);
    }

    [Fact]
    public void AMultiplicacaoDePolinomiosConfereNaMao()
    {
        // (x + 1)(x - 1) = x^2 - 1
        var produto = Polinomio.Fator(new Racional(-1)) * Polinomio.Fator(Racional.Um);
        Assert.Equal(2, produto.Grau);
        Assert.Equal(new Racional(-1), produto.Coeficientes[0]);
        Assert.Equal(Racional.Zero, produto.Coeficientes[1]);
        Assert.Equal(Racional.Um, produto.Coeficientes[2]);
    }

    /// <summary>
    /// A base de Lagrange vale UM no proprio no e ZERO em todos os outros. E a
    /// propriedade que define a forma, e ela nao depende dos valores.
    /// </summary>
    [Fact]
    public void ABaseDeLagrangeValeUmNoProprioNo()
    {
        Racional[] x = [-2, -1, 0, 1, 3];

        for (var i = 0; i < x.Count(); i++)
        {
            var basei = Lagrange.Base(x, i);
            for (var j = 0; j < x.Length; j++)
                Assert.Equal(i == j ? Racional.Um : Racional.Zero, basei.Avaliar(x[j]));
        }
    }

    [Fact]
    public void OPolinomioPassaPorTodosOsPontos()
    {
        Racional[] x = [-2, -1, 0, 1, 2];
        Racional[] y = [7, 1, 3, 5, 11];

        var lagrange = Lagrange.Interpolar(x, y);
        var newton = Newton.Interpolar(x, y);

        for (var i = 0; i < x.Length; i++)
        {
            Assert.Equal(y[i], lagrange.Avaliar(x[i]));
            Assert.Equal(y[i], newton.Avaliar(x[i]));
        }
    }

    /// <summary>
    /// As duas formas devolvem o MESMO polinomio, coeficiente a coeficiente.
    /// </summary>
    [Fact]
    public void AsDuasFormasDaoOMesmoPolinomio()
    {
        for (var pontos = 2; pontos <= 9; pontos++)
        {
            var x = Enumerable.Range(0, pontos).Select(i => (Racional)(i - pontos / 2)).ToArray();
            var y = Enumerable.Range(0, pontos).Select(i => (Racional)(i * i * 3 + 7)).ToArray();

            Assert.True(Lagrange.Interpolar(x, y).MesmoQue(Newton.Interpolar(x, y)), $"{pontos} pontos");
        }
    }

    /// <summary>
    /// Interpolar um POLINOMIO com pontos suficientes devolve ele mesmo, em
    /// aritmetica exata. Sem arredondamento nenhum, o erro e zero de verdade.
    /// </summary>
    [Fact]
    public void InterpolarUmPolinomioDevolveEleMesmo()
    {
        // 3x^2 + 1 em cinco pontos.
        Racional[] x = [-2, -1, 0, 1, 2];
        var y = x.Select(v => new Racional(3) * v * v + Racional.Um).ToArray();

        var achado = Lagrange.Interpolar(x, y);
        var esperado = new Polinomio([Racional.Um, Racional.Zero, new Racional(3)]);

        Assert.True(achado.MesmoQue(esperado), achado.ToString());
    }

    [Fact]
    public void OGrauNaoPassaDoNumeroDePontosMenosUm()
    {
        for (var pontos = 2; pontos <= 8; pontos++)
        {
            var x = Enumerable.Range(0, pontos).Select(i => (Racional)i).ToArray();
            var y = Enumerable.Range(0, pontos).Select(i => (Racional)(i * 7 % 5)).ToArray();
            Assert.True(Lagrange.Interpolar(x, y).Grau <= pontos - 1);
        }
    }

    [Fact]
    public void NoRepetidoReclama()
    {
        Racional[] x = [1, 2, 1];
        Racional[] y = [1, 2, 3];
        Assert.Throws<ArgumentException>(() => Lagrange.Interpolar(x, y));
    }

    [Fact]
    public void AQuantidadeDeValoresPrecisaBater()
    {
        Racional[] x = [1, 2, 3];
        Racional[] y = [1, 2];
        Assert.Throws<ArgumentException>(() => Lagrange.Interpolar(x, y));
        Assert.Throws<ArgumentException>(() => Newton.Interpolar(x, y));
    }

    /// <summary>
    /// Acrescentar um ponto a tabela de Newton da o mesmo que refazer tudo, e e
    /// isso que a forma compra.
    /// </summary>
    [Fact]
    public void AcrescentarUmPontoDaOMesmoQueRefazerTudo()
    {
        Racional[] x = [0, 1, 2, 3, 4];
        Racional[] y = [1, 4, 13, 28, 49];

        var parciaisX = x.Take(4).ToArray();
        var parciaisY = y.Take(4).ToArray();
        var parciais = Newton.Diferencas(parciaisX, parciaisY);

        var acrescentado = Newton.Acrescentar(x, y, parciais);
        var refeito = Newton.Diferencas(x, y);

        Assert.Equal(refeito, acrescentado);
    }

    /// <summary>
    /// Um ponto a mais custa LINEAR e refazer custa quadratico, entao a vantagem
    /// CRESCE com o numero de pontos. Ela nao e grande com dez pontos, e e isso
    /// que a conta diz: 10 contra 45. Com cem, e 100 contra 4.950.
    /// </summary>
    [Fact]
    public void AVantagemDeAcrescentarCresceComOsPontos()
    {
        double anterior = 0;
        foreach (var pontos in new[] { 10, 50, 100, 500 })
        {
            var vantagem = Newton.Divisoes(pontos) / (double)Newton.DivisoesDeUmPontoAMais(pontos);
            Assert.True(vantagem > anterior, $"com {pontos} pontos a vantagem deveria crescer");
            Assert.True(vantagem > 1);
            anterior = vantagem;
        }

        Assert.Equal(45, Newton.Divisoes(10));
        Assert.Equal(10, Newton.DivisoesDeUmPontoAMais(10));
    }
}
