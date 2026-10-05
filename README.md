# interpolacao

Interpolação do zero em C# e .NET 8: Lagrange e Newton em coeficientes racionais
exatos, nós uniformes e de Chebyshev, spline cúbica natural, e a constante de
Lebesgue. O assunto é a **escolha dos nós**, que quase todo material trata como
detalhe.

```
$ dotnet medidor.dll runge

  pontos     nos uniformes    nos de Chebyshev     spline cubica
       5         4.38E-001           4.02E-001         2.79E-001
       9         1.05E+000           1.71E-001         5.61E-002
      13         3.66E+000           6.92E-002         6.91E-003
      17         1.44E+001           3.26E-002         3.75E-003
      21         5.98E+001           1.53E-002         3.18E-003
      25         2.58E+002           6.96E-003         1.93E-003
```

A primeira coluna **cresce**: com 25 pontos o erro é de 258, numa função cujo
valor nunca passa de 1. Carl Runge mostrou isso em 1901 e continua sendo
contraintuitivo: mais informação sobre a função dá uma aproximação pior.

A culpa não é da função, que é lisa, nem do método, que é exato. É dos **nós**.

## Por que Chebyshev funciona

O erro da interpolação tem um fator que é o produto das distâncias a todos os
nós, e esse produto explode perto das bordas quando eles são uniformes. Os nós de
Chebyshev ficam **apertados nas bordas** e espalhados no meio, que é o contrário
do que a intuição sugere, e é justamente isso que segura o produto.

```
$ dotnet medidor.dll lebesgue

  pontos       nos uniformes      nos de Chebyshev           razao
       5           2.21E+000                 1.570        1.4E+000
      15           2.83E+002                 2.265        1.2E+002
      25           1.38E+005                 2.589        5.3E+004
      30           3.45E+006                 2.700        1.3E+006
```

A constante de Lebesgue responde uma pergunta que o erro medido não responde: se
os valores tiverem um errinho de medição, quanto do errinho aparece no polinômio?
A resposta é "até esta constante vezes", e ela só depende dos **nós**.

Com nós uniformes ela cresce de maneira exponencial: com 30 pontos, um erro de
medição de um milésimo pode virar um erro de 3.450 no resultado.

## O que as medidas me corrigiram

**O polinômio de grau quatro deveria sair exato, e não sai.** Interpolar um
polinômio de grau quatro com nove pontos devolve ele mesmo, então o erro deveria
ser zero. Ele dá 2e-5 com nove nós e 7e-4 com dezessete, e **piora** com mais
pontos.

O motivo não é o método: é que os nós são arredondados para racionais de
denominador cem mil antes de entrar na conta exata, e esse arredondamento é um
errinho de 5e-6 nos valores. A constante de Lebesgue com dezessete nós uniformes
passa de mil, e mil vezes 5e-6 é justamente a ordem do que aparece. A constante
fez exatamente o que ela promete, dentro da minha própria medida.

**A convergência da spline não é monótona.** Eu escrevi que mais pontos sempre
melhora, e o erro cai de 1,3e-1 para 2,5e-3 e depois **sobe** para 3,7e-3 antes de
voltar a cair.

A causa é a condição de ponta: "natural" quer dizer curvatura zero nas bordas, e a
função de Runge não tem curvatura zero em -1 nem em 1. Esse erro de fronteira não
melhora com mais pontos no meio, e a partir de certo tamanho ele é o que domina. A
spline continua ganhando do polinômio com folga; o que cai é a frase.

**E nem todo erro de interpolação é culpa dos nós.**

```
$ dotnet medidor.dll funcoes

funcao                    pontos        uniforme       Chebyshev
Runge, 1/(1+25x^2)             9       1.05E+000       1.71E-001
                              17       1.44E+001       3.26E-002

valor absoluto                 9       3.16E-001       6.70E-002
                              17       1.11E+001       3.52E-002

exponencial                    9       1.34E-005       1.46E-005
                              17       4.90E-004       2.13E-005
```

O valor absoluto tem um **bico**, e nenhum polinômio aproxima bico direito:
Chebyshev melhora e não resolve. A exponencial é fácil para os dois. A linha de
Runge é a única em que a troca dos nós muda tudo.

## Duas formas, o mesmo polinômio

O polinômio interpolador é **único**, então Lagrange e Newton são duas maneiras de
escrever a mesma coisa. Em ponto flutuante os coeficientes saem parecidos e não
iguais, e daí não dá para saber se a diferença é arredondamento ou erro de
implementação. Em racionais exatos eles **têm** que bater, e um teste sobre isso ou
passa ou não passa.

```
$ dotnet medidor.dll formas

  pontos     Lagrange, do zero     Newton, do zero    Newton, +1 ponto
       5                    25                  10                   5
      20                   400                 190                  20
     100                10,000               4,950                 100
```

A última coluna é o motivo de a forma de Newton existir. Acrescentar um ponto
custa **uma** diferença dividida nova; em Lagrange, cada base tem um fator por nó,
então um ponto novo joga fora o trabalho inteiro.

## A spline resolve por outro caminho

Chebyshev mantém **um** polinômio e muda onde estão os nós. A spline mantém os nós
onde estiverem e abandona a ideia de um polinômio só: como o grau de cada pedaço é
sempre três, não importa quantos pontos haja, a oscilação não tem para onde
crescer.

Duas conferências dos testes não olham valor nenhum, e são elas que distinguem uma
spline de uma curva qualquer que passa pelos pontos: as emendas têm que ser
**lisas** (o maior salto na inclinação dá 4e-16) e a curvatura nas duas pontas tem
que ser **zero**. Uma implementação errada passa por todos os pontos do mesmo
jeito.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Racional.cs` | a fração de inteiro grande |
| `Polinomio.cs` | coeficientes exatos, Horner, e os zeros do fim cortados |
| `Formas.cs` | Lagrange, Newton, e o ponto acrescentado |
| `Nos.cs` | uniformes, Chebyshev, Lebesgue e as funções de teste |
| `Spline.cs` | a spline natural, e a medida de erro amostrada |

## Como rodar

```
dotnet test testes/Interpolacao.Testes/Interpolacao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `runge`, `lebesgue`, `funcoes`, `formas`, `spline` e `tudo`.

## Licença

MIT.
