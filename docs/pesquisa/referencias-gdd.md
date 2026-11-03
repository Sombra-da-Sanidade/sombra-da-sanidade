# Jogos de referência do GDD

GDD seção 1: Darkwood, Resident Evil 2, Alien: Isolation, SOMA, Signalis, Yuppie Psycho. Alien: Isolation está em [ritmo-e-tensao.md](ritmo-e-tensao.md).

A maior parte das fontes abaixo é crítica e entrevista, não documento de design. Tratar como recepção do público, não como intenção dos desenvolvedores, salvo quando citado.

## Darkwood

Fontes: [Wikipedia](https://en.wikipedia.org/wiki/Darkwood), [press kit](https://www.darkwoodgame.com/presskit/), [wiki de fãs: tempo](https://breezewiki.discard.no/darkwood/wiki/Time).

- Ciclo dia/noite: de dia explora e coleta; à noite não sai do esconderijo e defende até amanhecer.
- Sobreviver à noite dá reputação com o comerciante (recompensa concreta). Morrer de noite faz acordar sem ela.
- Sem esconderijo com forno aceso, monstros aparecem e caçam o jogador.
- Lanternas e tochas gastam durabilidade mesmo com o tempo parado: luz é recurso.
- Visão top-down com cone de visão, a referência mais próxima da nossa câmera.

**Para nós:** a Zona Segura do disjuntor é o "esconderijo"; a luz da lanterna é o recurso que se esgota. Recompensar quem sobrevive a um trecho difícil (como a reputação) reforça a recompensa clara pedida pelo professor.

## Resident Evil 2 (remake): Mr. X

Fontes: [The Ringer](https://www.theringer.com/2019/01/29/video-games/resident-evil-2-remake-video-game), [GamingBolt](https://gamingbolt.com/seven-years-later-mr-x-is-still-resident-evils-scariest-stalker), [Unwinnable](https://unwinnable.com/?p=73421), [Medium: "How to Design an Alpha Antagonist"](https://medium.com/super-jump/the-horrific-appeal-of-mr-x-af88c622faed).

- Delegacia pequena e corredores estreitos: o encontro é quase inevitável, mesmo sem perseguição ativa.
- Mr. X anda pelo mapa procurando o jogador em vez de aparecer em cenas roteirizadas.
- Passos pesados e portas batendo avisam onde ele está: tensão com aviso justo.
- Dá para atordoá-lo, mas custa munição escassa. Parecido com o nosso EMP (-40 de bateria).
- Crítica: colocar o stalker em todos os cenários cansou parte do público.

**Para nós:** o som do monstro precisa ser legível (passos, respiração por distância). O EMP deve continuar caro para ser decisão, não rotina.

## SOMA

Fontes: [PC Gamer, entrevista com Thomas Grip sobre o Safe Mode](https://www.pcgamer.com/frictional-on-designing-somas-new-monster-free-safe-mode), [The Ringer](https://www.theringer.com/2017/12/08/video-games/soma-safe-mode-frictional-games), [Game Developer](https://gamedeveloper.com/design/how-frictional-pacified-i-soma-i-s-monsters-without-sacrificing-the-scary).

- O Safe Mode veio de um mod de fã. Os monstros continuam lá como atmosfera e ainda reagem se o jogador abusar.
- Grip: os encontros com monstro não acrescentavam ao tema; o horror do SOMA é existencial.
- Grip acha que os jogos dependem demais de "monstro persegue jogador".

**Para nós:** o horror psicológico (sanidade, Revelação pela Loucura, logs da Dra. Vance) tem que carregar o jogo junto com a perseguição. Um modo com o monstro mais brando é uma opção de acessibilidade barata para o fim do projeto.

## Signalis

Fontes: [DreadCentral, save points](https://www.dreadcentral.com/editorials/493266/signalis-sows-terror-through-clever-save-point-design/), [TechRadar](https://techradar.com/features/signalis-turns-a-divisive-survival-horror-trope-into-its-best-feature), [GamesRadar (crítica)](https://gamesradar.com/signalis-review).

- Inventário de 6 itens, a mecânica mais polêmica do jogo, e o jogo nunca relaxa essa regra.
- Salas de save com baú são a válvula de alívio. Não há salvamento automático.
- Crítica: as idas e vindas até o baú podem ficar tediosas.

**Para nós:** escassez cria tensão, mas logística demais cansa. O checkpoint ao fim de cada ala (GDD 10) já é o "save room"; vale anunciar como recompensa ("Progresso salvo").

## Yuppie Psycho

Fontes: [Wikipedia](https://en.wikipedia.org/wiki/Yuppie_Psycho), [TheSixthAxis](https://www.thesixthaxis.com/2019/04/29/yuppie-psycho-review/), [DualShockers](https://www.dualshockers.com/yuppie-psycho-is-a-survivor-horror-experience-set-inside-a-corporate-office/).

- Não achamos análise de design das perseguições, só premissa e críticas.
- Loop: achar pistas, resolver enigmas, abrir caminhos sem ser pego pela bruxa.
- Influências citadas pelos devs: Silent Hill, Deadly Premonition, Yomawari. Arte: Shintaro Kago, Junji Ito.
- Críticas: save limitado às vezes frustra; alguns trechos se arrastam.

**Para nós:** é a principal referência de arte (ver `docs/arte/guia-de-estilo.md`) e de tom corporativo (Aethelgard Dynamics). O ponto de design é o mesmo do Signalis: save escasso precisa ser justo.
