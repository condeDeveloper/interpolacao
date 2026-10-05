using System.Numerics;

namespace Conde.Interpolacao;

/// <summary>
/// Uma fracao de inteiro grande, sempre reduzida.
///
/// Ela existe porque o polinomio interpolador e UNICO: dados n+1 pontos com
/// abscissas diferentes, existe um unico polinomio de grau no maximo n que passa
/// por todos. As formas de Lagrange e de Newton sao duas maneiras de escrever o
/// MESMO polinomio.
///
/// Em ponto flutuante as duas dao coeficientes parecidos e nao iguais, e dai nao
/// da para saber se a diferenca e arredondamento ou erro de implementacao. Em
/// aritmetica exata elas tem que bater coeficiente a coeficiente, e um teste
/// sobre isso ou passa ou nao passa.
/// </summary>
public readonly struct Racional : IEquatable<Racional>
{
    private readonly BigInteger baixo;

    public Racional(BigInteger cima, BigInteger baixo)
    {
        if (baixo.IsZero) throw new DivideByZeroException("racional com denominador zero");

        if (baixo.Sign < 0)
        {
            cima = -cima;
            baixo = -baixo;
        }

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);
        if (!divisor.IsZero && !divisor.IsOne)
        {
            cima /= divisor;
            baixo /= divisor;
        }

        Cima = cima;
        this.baixo = baixo;
    }

    public Racional(BigInteger inteiro) : this(inteiro, BigInteger.One) { }

    public BigInteger Cima { get; }

    /// <summary>O denominador, com UM para o valor construido pelo default.</summary>
    public BigInteger Baixo => baixo.IsZero ? BigInteger.One : baixo;

    public static Racional Zero => new(BigInteger.Zero);
    public static Racional Um => new(BigInteger.One);

    public bool EhZero => Cima.IsZero;

    public static Racional De(long cima, long baixo) => new(cima, baixo);

    public static implicit operator Racional(int inteiro) => new(inteiro);

    public static Racional operator +(Racional a, Racional b) =>
        new(a.Cima * b.Baixo + b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Racional operator -(Racional a, Racional b) =>
        new(a.Cima * b.Baixo - b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Racional operator -(Racional a) => new(-a.Cima, a.Baixo);

    public static Racional operator *(Racional a, Racional b) => new(a.Cima * b.Cima, a.Baixo * b.Baixo);

    public static Racional operator /(Racional a, Racional b) => new(a.Cima * b.Baixo, a.Baixo * b.Cima);

    public double Valor()
    {
        if (BigInteger.Abs(Cima) < BigInteger.Pow(2, 900) && Baixo < BigInteger.Pow(2, 900))
            return (double)Cima / (double)Baixo;

        var sinal = Cima.Sign;
        return sinal * Math.Exp(BigInteger.Log(BigInteger.Abs(Cima)) - BigInteger.Log(Baixo));
    }

    public bool Equals(Racional outro) => Cima == outro.Cima && Baixo == outro.Baixo;

    public override bool Equals(object? obj) => obj is Racional outro && Equals(outro);

    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);

    public static bool operator ==(Racional a, Racional b) => a.Equals(b);

    public static bool operator !=(Racional a, Racional b) => !a.Equals(b);

    public override string ToString() => Baixo.IsOne ? Cima.ToString() : $"{Cima}/{Baixo}";
}
