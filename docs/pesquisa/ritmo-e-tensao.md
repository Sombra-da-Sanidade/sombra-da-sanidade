# Ritmo e tensão: Alien: Isolation, Left 4 Dead e Hamlet

Como jogos com perseguidor controlam a tensão sem cansar o jogador. Base para a curva dente-de-serra do GDD seção 7.

## Alien: Isolation

Fonte: Tommy Thompson, ["The Perfect Organism: The AI of Alien: Isolation"](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation), Game Developer, 2017, com informações de Andy Bray (Creative Assembly). Continuação: ["Revisiting the AI of Alien: Isolation"](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).

- **Dois níveis de IA.** O Director sabe sempre onde o jogador está e de tempos em tempos manda o alien para perto. O alien em si só usa sensores (passos, ruído, visão). O Director dá a direção, o alien precisa achar.
- **Menace gauge.** Sobe quando o alien está a poucos passos, quando o jogador o vê e quando ele está perto no rastreador. Sobe menos se ele estiver perto mas em outro cômodo.
- **Recuo no pico.** Quando o medidor enche, o Director manda o alien para áreas vizinhas ou para os dutos. A tensão alterna com alívio para o jogador conseguir progredir.
- **Comportamentos destravados aos poucos.** A behavior tree tem partes bloqueadas que abrem com o progresso, dando a impressão de que o alien aprende. As condições **não incluem mortes do jogador**, para não parecer injusto. Na versão final, os destravamentos são por pontos da campanha.
- **Sem trapaça.** O alien teleporta só duas vezes em uma campanha de 12 a 18 h, e só em cutscene. Designers marcam áreas que ele nunca checa, para evitar mortes injustas.
- **Busca com dúvida.** Visita os pontos de interesse em ordem subótima e às vezes volta. Parece que está procurando, não que sabe.
- **"Olhos na nuca".** Raycasts curtos para trás impedem que o jogador fique colado nas costas dele.

## Left 4 Dead (AI Director)

Fonte: Michael Booth, ["Replayable Cooperative Game Design: Left 4 Dead"](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf), GDC 2009.

- **Intensidade emocional** de cada jogador como um número. Sobe quando ele leva dano (proporcional) e quando cai. Decai para zero com o tempo, **mas não decai enquanto há inimigos atacando**.
- **Ciclo de ritmo:**
  1. **Build Up:** população de ameaças completa até a intensidade cruzar o pico.
  2. **Sustain Peak:** mantém por 3 a 5 s depois do pico.
  3. **Peak Fade:** população mínima, espera a ação acabar naturalmente.
  4. **Relax:** população mínima por 30 a 45 s ou até o grupo avançar o suficiente.
- Combate constante cansa, inatividade longa entedia. Picos e vales imprevisíveis seguram o jogador.
- **Structured unpredictability:** possibilidades escolhidas em tempo de execução, dentro de restrições desenhadas. "Baixa probabilidade + alto drama = memorável." Resistir à vontade de fazer o jogador ver tudo, sempre.

## Hamlet (dificuldade dinâmica)

Fonte: Robin Hunicke e Vernell Chapman, ["AI for Dynamic Difficulty Adjustment in Games"](https://users.cs.northwestern.edu/~hunicke/pubs/Hamlet.pdf), Northwestern University. Panorama mais recente: [Exploring Dynamic Difficulty Adjustment in Videogames (arXiv 2007.07220)](https://arxiv.org/pdf/2007.07220).

- Jogo é chato quando fácil demais e frustrante quando difícil demais. O objetivo é manter o jogador no **flow channel** (Csikszentmihalyi).
- O Hamlet observa a **economia de itens** (vida, munição) e prevê quando o jogador está "se debatendo": perto de um estado em que os recursos dele não dão mais conta.
- Quando prevê isso, intervém ajustando oferta e demanda (mais item, menos inimigo).
- Cuidados: ajuste moderado para não criar efeito elástico, e o jogador não deve perceber. Ajustar dificuldade tira controle do designer, então só onde for medido.

Ferramenta relacionada: [PaceMaker: A Practical Tool for Pacing Video Games (arXiv 2408.15001)](https://arxiv.org/pdf/2408.15001), sobre ritmo por ciclos de regras.

## Lições para o nosso jogo

1. Separar **diretor de tensão** (sabe tudo, decide o ritmo) do **monstro** (só sente luz e ruído). Combina com "monstro cego" do GDD.
2. Recuo obrigatório depois de um pico: é o vale do dente-de-serra.
3. O monstro fica mais perigoso por ala, nunca por morte do jogador.
4. Imprevisibilidade estruturada no loot e nas patrulhas, para rejogar não ser igual.
5. Dificuldade dinâmica, se houver, pela economia (bateria e sedativo nos armários) e de forma discreta.
