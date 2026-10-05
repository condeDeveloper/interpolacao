namespace Conde.Interpolacao;

/// <summary>
/// Um polinomio em coeficientes RACIONAIS, do termo constante para cima.
///
/// Ele e o juiz do repositorio. O polinomio interpolador e unico, entao Lagrange
/// e Newton tem que devolver exatamente este objeto, coeficiente a coeficiente.
/// </summary>
public sealed class Polinomio
{
    public Polinomio(IEnumerable<Racional> coeficientes)
    {
        ArgumentNullException.ThrowIfNull(coeficientes);
        var lista = coeficientes.ToList();

        // Cortar os zeros do fim NAO e cosmetica: sem isso, dois polinomios
        // iguais com listas de tamanhos diferentes deixam de ser iguais, e o
        // teste que compara Lagrange com Newton falha por motivo nenhum.
        while (lista.Count > 1 && lista[^1].EhZero) lista.RemoveAt(lista.Count - 1);

        Coeficientes = lista;
    }

    public IReadOnlyList<Racional> Coeficientes { get; }

    public int Grau => Coeficientes.Count - 1;

    public static Polinomio Zero => new([Racional.Zero]);

    public static Polinomio Constante(Racional valor) => new([valor]);

    /// <summary>O polinomio x menos a raiz dada, que e o tijolo de Lagrange.</summary>
    public static Polinomio Fator(Racional raiz) => new([-raiz, Racional.Um]);

    public static Polinomio operator +(Polinomio a, Polinomio b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var tamanho = Math.Max(a.Coeficientes.Count, b.Coeficientes.Count);
        var saida = new Racional[tamanho];
        for (var i = 0; i < tamanho; i++)
            saida[i] = (i < a.Coeficientes.Count ? a.Coeficientes[i] : Racional.Zero)
                       + (i < b.Coeficientes.Count ? b.Coeficientes[i] : Racional.Zero);
        return new Polinomio(saida);
    }

    public static Polinomio operator *(Polinomio a, Polinomio b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var saida = new Racional[a.Coeficientes.Count + b.Coeficientes.Count - 1];
        for (var i = 0; i < a.Coeficientes.Count; i++)
            for (var j = 0; j < b.Coeficientes.Count; j++)
                saida[i + j] += a.Coeficientes[i] * b.Coeficientes[j];
        return new Polinomio(saida);
    }

    public static Polinomio operator *(Polinomio a, Racional k)
    {
        ArgumentNullException.ThrowIfNull(a);
        return new Polinomio(a.Coeficientes.Select(c => c * k));
    }

    /// <summary>
    /// O valor em aritmetica exata, por Horner.
    ///
    /// Horner nao e so economia de multiplicacoes: avaliar termo a termo calcula
    /// x elevado ao grau, que em ponto flutuante estoura ou perde digitos muito
    /// antes de o polinomio ter grau interessante.
    /// </summary>
    public Racional Avaliar(Racional x)
    {
        var saida = Racional.Zero;
        for (var i = Coeficientes.Count - 1; i >= 0; i--) saida = saida * x + Coeficientes[i];
        return saida;
    }

    /// <summary>O valor em ponto flutuante, tambem por Horner.</summary>
    public double Avaliar(double x)
    {
        double saida = 0;
        for (var i = Coeficientes.Count - 1; i >= 0; i--) saida = saida * x + Coeficientes[i].Valor();
        return saida;
    }

    public bool MesmoQue(Polinomio outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Coeficientes.Count == outro.Coeficientes.Count
               && Coeficientes.Zip(outro.Coeficientes).All(p => p.First == p.Second);
    }

    public override string ToString()
    {
        if (Coeficientes.Count == 1) return Coeficientes[0].ToString();

        var partes = new List<string>();
        for (var i = Coeficientes.Count - 1; i >= 0; i--)
        {
            if (Coeficientes[i].EhZero) continue;
            partes.Add(i switch
            {
                0 => Coeficientes[i].ToString(),
                1 => $"{Coeficientes[i]}x",
                _ => $"{Coeficientes[i]}x^{i}",
            });
        }
        return partes.Count == 0 ? "0" : string.Join(" + ", partes);
    }
}
