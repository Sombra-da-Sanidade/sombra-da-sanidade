# Sound design e trilha sonora

Plano do áudio do jogo inteiro para o RC (issue #96). Complementa [planejamento-rc.md](../planejamento-rc.md). Referências e resumo das fontes: `docs/referencias/audio/` (local).

## 1. Pilares

1. **Som é informação de jogo.** O Espécime é cego e caça pelo som: o jogador precisa ouvir o mesmo que o monstro (passos, skill check, servidor). Todo som que entra no `NoiseBus` tem um som audível equivalente, com volume proporcional ao raio.
2. **O monstro soa de onde ele está.** Nunca tocar som de monstro em posição falsa quando a sanidade está alta (lição do Alien: Isolation, em que o som nos dutos vem do alien de verdade). Mentir é privilégio da sanidade baixa.
3. **Silêncio é recurso.** A maior parte do tempo é baixa e esparsa, para que um rangido isolado conte uma história. Zona Segura é quase silêncio; o susto vem do contraste.
4. **Sanidade é ouvida, não lida.** HUD diegético (GDD 8): batimento, respiração, zumbido e abafamento substituem a barra. Abaixo de 35, alucinações usam o mesmo som dos eventos reais.
5. **Presente frio, passado abafado.** Memórias: fita gasta (wow/flutter), passa-baixa, voz da Vance limpa e harmonia maior quebrada. Espelha a paleta verde das memórias.
6. **Timers só pelo ouvido.** O GDD proíbe número de timer. A purga, o fim da Zona Segura e a bateria fraca são comunicados por som.

## 2. Restrição técnica: WebGL

Confirmado na documentação da Unity 6 (Audio in Web):

| Funciona no Web | Não funciona |
|---|---|
| `AudioSource` (volume, pitch positivo, loop, rolloff, `PlayScheduled`, `time`) | Efeitos do `AudioMixer` (passa-baixa, reverb, distorção, eco) |
| `AudioMixer` com grupos, só volume via `SetFloat` | `OnAudioFilterRead`, componentes de filtro |
| `AudioClip.Create` sem stream | Áudio antes do primeiro clique do jogador |

Consequências:

1. **Todo processamento é feito fora do jogo.** Cada som com variação de "espaço" é exportado em versões (`_dry`, `_muffled`, `_memory`). O jogo só faz crossfade de volume entre elas.
2. **Oclusão = troca de versão.** Raycast 2D até a fonte: com parede no meio, crossfade para `_muffled` e -6 dB.
3. **Snapshots viram curvas de volume** por grupo, aplicadas pelo nosso `AudioDirector` (snapshot do mixer só muda volume no Web, então tanto faz; o código não depende dele).
4. **Clips gerados em runtime** (batimento, bipes, sirene) continuam válidos, mas migram para WAV pré-renderados para ter o mesmo som em todo lugar.
5. **Clique antes do som:** o Menu já exige clique; o drone do menu começa só depois dele.

Medido no navegador na #100 ([ADR 0005](../decisoes/0005-audio-no-webgl.md)): 4 stems com `PlayScheduled` ficam sincronizados (deriva máxima de 3,7 ms), crossfade e `AudioClip.Create` funcionam, `OnAudioFilterRead` não é chamado.

## 3. Arquitetura

| Peça | Responsabilidade |
|---|---|
| `AudioMixer` `Sombra.mixer` | Grupos Master → Music, Ambience, SFX, Monster, Voice, UI. Volumes expostos para o menu de pausa (PlayerPrefs) |
| `SoundEvent` (ScriptableObject) | Lista de clips (3 a 8 takes), faixa de pitch ±, faixa de volume, grupo, distância mín./máx., raio de ruído no `NoiseBus`, cooldown, variante abafada |
| `AudioService` | `Play(SoundEvent, posição)` com pool de `AudioSource`; evita repetir o mesmo take duas vezes seguidas |
| `AudioDirector` | Lê `TensionDirector`, sanidade, Zona Segura e cutscene; mistura as camadas da música e os grupos por estado |
| `MusicLayers` | 4 stems da mesma duração começados juntos com `PlayScheduled`; só o volume de cada um muda |
| `AmbienceZone` | Trigger 2D com loop de ambiente e eventos aleatórios posicionados (Darkwood) |
| `AudioOcclusion` | Raycast 2D a cada 0,1 s nas fontes de monstro e eventos importantes |

Espacialização 2D: `spatialBlend` 0,8, rolloff linear, distância máx. 14 unidades para monstros e 8 para objetos; pan estéreo pela posição horizontal. Fontes de monstro também ganham `_far` (só grave e textura) para distância maior que 10.

## 4. Trilha sonora

### Ideia central: a contagem

A Vance diz em M1: *"Conte de 10 para trás, Thomas."* A trilha inteira nasce de **um motivo de 4 notas descendentes** (a contagem, ex.: Lá-Sol-Fá-Mi em Ré menor).

| Motivo | Onde | Forma |
|---|---|---|
| **Contagem** (Thomas) | Menu, memórias, Revelação, finais | Celesta/caixinha de música, lenta. No menu está **invertida** (ascendente): o jogador só reconhece a versão certa na Revelação, quando ela soa descendente com a voz da Vance |
| **Hipnos** (experimento) | Logs, câmeras, Eclusa | 2 notas a um trítono, como bipe de equipamento médico |
| **Aethelgard** (empresa) | Briefing C0, muzak da Ala Executiva, final "Amostra entregue" | Jingle corporativo alegre de 5 notas, anos 90, que vai se corrompendo |
| **Vance** | Logs e C3 | Piano com fita gasta; na C3 o mesmo piano sem chiado (ela está viva) |

### Música por fase (camadas verticais)

Cada fase tem 4 stems em loop de mesmo tamanho:

| Camada | Entra quando | F1 Ala Médica | F2 Ala de Energia | F3 Ala Executiva |
|---|---|---|---|---|
| L0 Ambiente | sempre | Ventilação, lâmpadas zumbindo, gotas | Hum de 60 Hz da rede elétrica com harmônicos, estalos de relé | Ar-condicionado, relógio, muzak corrompido baixo vindo de alto-falantes |
| L1 Drone | exploração | Drone grave em Ré, quase imóvel | Drone + zumbido elétrico que oscila | Drone + muzak tocado meio tom abaixo |
| L2 Pulso | tensão média (monstro investigando, câmera detectando) | Batida grave a 50 bpm | Pulso de relé a 60 bpm | Os dois pulsos sobrepostos, defasados |
| L3 Perseguição | monstro em perseguição | Percussão metálica, cordas raspadas | Ruído elétrico + percussão | Tudo + motivo Hipnos repetido |

Regras de mix: L3 entra em 0,3 s e sai em 4 s (recuo lento, curva dente-de-serra do GDD 7). A música segue a linha de visão: se o monstro vem pelas costas, só L2 sobe, nunca L3 (Alien: Isolation). Na Zona Segura, tudo cai para L0 e entra "Sala segura".

#### Como ficou (#104)

Receitas em `audio-src/Music/ala_<ala>_l0..l3.json` e `eclusa_l0..l3.json`. As melodias são camadas `tone`, então não precisam do fluidsynth, e a percussão e as texturas usam gravações CC0 (`docs/audio/fontes.md`). Saída em `Assets/_Project/Audio/Music/`. O builder monta os `MusicSet` `Medica`, `Energia`, `Executiva` e `Eclusa` e liga cada um no `AudioConfig`.

- **Loop:** todos os stems têm exatamente 48 s (2.116.800 amostras). Isso dá 40 batidas a 50 bpm, 48 a 60, 60 a 75, 96 a 120 e 120 a 150, então o loop emenda na batida e as 4 camadas nunca saem de sincronia.
- **Volume por camada:** cada stem tem alvo próprio de loudness (`"lufs"`): L0 -21, L1 -20, L2 -21, L3 -18. Somadas e com o `layerVolume` 0,8:
  - exploração: cerca de -19,5 LUFS
  - perseguição: cerca de -16,5 LUFS
- **Teto de pico:** cada stem tem teto de pico menor (`"peak"`, de -6 a -8 dBTP), porque as batidas das camadas caem no mesmo instante. Somadas, as 4 ficam perto de 0 dBFS.
- **Sem cena alterada:** o `AudioDirector` cria a `MusicLayers` em runtime pela ala do `WingCheckpoint`.
- **Game Over:** `MusicLayers.Cut()` faz o corte seco.
- **Convivência com os sons de jogo (#106):**
  - **Pulso:** o L2 não usa batimento de coração. O coração é do `SanityHeartbeat` (65 a 160 bpm), e dois corações em ritmos diferentes brigavam. O pulso das fases é tambor grave, chapa de metal e relé.
  - **Vitória:** na tela "Ala concluída", a música cai para só o ambiente.
  - **Vitória final:** o jingle de vitória (4,5 s) toca sozinho e só depois entra o laudo (`AudioConfig.laudoDelay`).

| Fase | L0 Ambiente | L1 Drone | L2 Pulso | L3 Perseguição |
|---|---|---|---|---|
| Médica | Ventilação, lâmpada baixa, gotas, chapa de metal assentando ao longe | Grave em Ré + pad Ré-Lá-Mi, Fá entrando no meio | Tambor grave a 50 bpm e chapa de metal a cada 2 batidas | Metal gravado a 100 bpm, tambor grave, raspados de metal, cordas em Ré e Ré# |
| Energia | Transformador (60 e 120 Hz em senoide), sino de metal gemendo bem grave, relés estalando | Pad aberto Ré-Lá-Dó e dois "fios cantando" agudos que vêm e vão | Relé (clique gravado) e chapa de metal a 60 bpm, grave longo a cada 4 s | Baixo saturado Ré-Ré-Ré-Ré-Fá-Ré-Sol#-Ré a 120 bpm, bumbo, metal nos tempos 2 e 4, raspado |
| Executiva | Ar-condicionado, relógio gravado, jingle Aethelgard baixinho no alto-falante a cada 24 s | Grave + pad Ré menor + jingle meio tom abaixo com wow de fita | Tambor a 50 bpm e relé a 60 bpm defasados (coincidem a cada 6 s) | Metal, bumbo, raspado, cordas e o bipe Hipnos (Ré6–Sol#5) a cada 2 s |
| Eclusa | Rugido grave da câmara, chiado de pressurização antes de cada troca de acorde, sino grave, Hipnos ao longe a cada 16 s | Progressão de 12 em 12 s: Ré m, Sib, Sol m, Lá maior (termina em suspense e recomeça), com sino na troca e grave seguindo a tônica | Tambor a 75 bpm e tique de contagem a 150 bpm (a saída está perto) | Baixo sincopado seguindo os acordes, tambores a 150 bpm, cordas em trêmolo, subida de ruído até cada troca, Hipnos a cada 1,6 s |

### Faixas fixas

Feitas na #103: receitas em `audio-src/Music/`, motivos em `audio-src/Music/Motivos/*.mid` (gerados por `tools/audio/motifs.py`), saída em `Assets/_Project/Audio/Music/`. Todas em -16 LUFS.

| Faixa | Arquivo | Duração | Descrição | Onde toca |
|---|---|---|---|---|
| Menu | `menu.ogg` | loop 61 s | Drone grave em Lá + contagem invertida (subindo) na celesta a cada 16 s | `MainMenuScreen` (troca o drone antigo da cena) |
| Sala segura | `sala_segura.ogg` | loop 56 s | Única faixa calma: pad em Ré menor, Bb, F, C com o motivo da Vance no piano, leve saturação de fita | `AudioDirector` na Zona Segura |
| Memória | `memoria_memory.ogg` | loop 28 s | Contagem descendo na celesta, versão de fita (wow/flutter, passa-baixa) | Cutscenes de memória (#88/#91) |
| Eclusa / purga | `purga.ogg` | **150 s exatos** | Pulso grave de 60 a 140 bpm, chimbal a partir de 60 s, drone subindo uma oitava, bipe Hipnos a cada 10 s. A sirene é a da `EscapeSequence` (código), não da faixa | `AudioDirector` com a purga, sincronizado com `EscapeSequence.Elapsed` |
| Revelação (C3) | `revelacao.ogg` | 45 s | Contagem descendo no piano pela primeira vez, cordas em Ré menor | Cutscene C3 (#91) |
| Final "Testemunha" | `final_testemunha.ogg` | 60 s | Contagem resolvida em Ré maior no piano, cordas, sem fita | Cutscene C4 (#91) |
| Final "Amostra entregue" | `final_amostra.ogg` | 60 s | Jingle Aethelgard alegre no vibrafone sobre drone que cresce: ironia | Cutscene C5 (#91) |
| Game Over | `game_over.ogg` | 6 s | Estalo, fita parando, zumbido de ouvido | `AudioDirector`, corte seco no mesmo quadro |
| Laudo | `laudo.ogg` | loop 38 s | Drone suave em Ré, impressora matricial, bipe Hipnos | `AudioDirector` na vitória da última fase |

Os loops ficam mais curtos que a `duration` da receita: o crossfade da emenda corta o fim. A faixa da sala segura ainda não faz o fade nos últimos 10 s da Zona Segura (o `Breaker` não expõe o tempo restante).

## 5. Efeitos sonoros por sistema

Cada linha é um `SoundEvent`. P1 é informação de jogo e vem primeiro.

| Sistema | Eventos | P |
|---|---|---|
| Thomas | Passos andando e correndo (3 pisos: azulejo, metal, carpete; 6 takes cada), respiração cansada após correr, dano/susto | 1 |
| Lux-90 | Liga, desliga, zumbido contínuo baixo, bateria fraca (piscar elétrico, abaixo de 20), esgotada, carga do EMP (0,4 s) e descarga | 1 |
| Espécime | Respiração na patrulha (perto/longe), estalo ao ouvir algo (virar a cabeça), passos pesados que aceleram com a velocidade, grito de perseguição, atordoado pelo EMP, ataque | 1 |
| Sonâmbulo | Rangido de ossos e arrasto **só enquanto se move**; no momento em que a luz o congela, o som corta em seco. Respiração entrecortada parado. Corrida no escuro | 1 |
| Câmera | Servo da varredura, bipe de detecção acelerando (0,6 s), alarme (diferente da sirene da purga), apagão (relé + luzes morrendo), desligar no disjuntor, curto do EMP | 1 |
| Skill check | Bipe de início, ponteiro (tique), good, great, erro (estalo + faísca), ruído do servidor > 50% | 1 |
| Disjuntor | Reparo em loop (4 s), estalo de religar, lâmpadas acendendo em cascata | 2 |
| HD | Ejeção mecânica (12 s em loop), trava, coleta | 2 |
| Itens | Abrir armário (vazio ou com item), pegar pilha, injeção do Adrena-Syn, Estabilizador, Módulo Neural | 2 |
| Mundo | Portas, eventos aleatórios por fase (canos, metal se acomodando, papel), elevador da C0 | 2 |
| Sanidade | Batimento em camadas (existe), respiração (existe), zumbido agudo abaixo de 15, alucinações (existem, migram para WAV) | 1 |
| Narrativa | Bipe da Vance (existe), bipe do Thomas uma oitava abaixo, glitch de fim de memória, digitação das legendas, cela da Vance abrindo | 3 |
| UI | Navegar, confirmar, voltar, escolha de upgrade, "Progresso salvo", transição CRT | 3 |

### Feito na #105

Receitas em `audio-src/{Player,Lux,Monster,SkillCheck,Camera}/`, ligadas no `Resources/GameSfx` e tocadas pelo `GameSfxHooks` (criado em toda cena de jogo, ouvindo os eventos dos sistemas; nenhuma cena muda). Nenhum desses sons emite ruído no `NoiseBus`: o que o Espécime ouve não mudou.

| Gancho | Sons |
|---|---|
| Passos do Thomas pela distância andada (0,85 u) | `passos_andar` (6 tomadas), `passos_correr` com Adrena-Syn |
| `FlashlightController.LitChanged`, `EmpFired`, `EmpFailed`, bateria < 20% | liga, desliga, zumbido aceso, bateria fraca (a cada 6 s), esgotada, EMP, EMP falho |
| Espécime: loop com `AudioOcclusion`, `StateChanged`, `Caught`, distância andada | respiração (mais alta na perseguição, some atordoado), estalo ao investigar, grito na perseguição, atordoado, passos, ataque |
| Sonâmbulo: estado, `Woke`, `Caught` | rangido só em `Moving` (corta no mesmo quadro ao congelar), respiração em `Frozen`, despertar, ataque |
| `SkillCheck.Warned`, `Resolved`, ponteiro | aviso, 8 tiques por varredura, bom, ótimo, falha |
| Câmera (#79, ainda sem gancho) | servo, detecção, alarme, apagão, curto do EMP |

Ainda não: passos por tipo de piso (as cenas não marcam o piso), respiração cansada.

### Feito na #106

Receitas em `audio-src/World/` e `audio-src/UI/`, no mesmo esquema da #105 (`GameSfx` + `GameSfxHooks`, nenhuma cena muda). Os menus tocam os sons de interface pelo `UiSfx`, porque o Menu não tem `GameSfxHooks`. Teste: **Sombra da Sanidade > Rodar teste dos sons de mundo**.

| Gancho | Som |
|---|---|
| `Breaker.StateChanged` / `Burnt` | `disjuntor_liga` (alavanca, relés, lâmpadas piscando até acender), `disjuntor_desliga`, `disjuntor_queima` |
| `DriveServer.Ejected` / `GameManager.DriveCollected` | `hd_ejeta` (motor e trava), `hd_coleta` (estalo magnético e dois bipes) |
| `Locker.Opened` | `armario_abre` (3 tomadas); com pilha, também `item_pega` |
| `AdrenaSynEffect` ligando, `LithiumStabilizer.Applied` (Estabilizador e sedativo) | `item_injecao` |
| `FlareThrower.Count` subindo / descendo | `item_pega` / `sinalizador_acende`; depois, `sinalizador_queimando` em loop no chão enquanto `SignalFlare.IsBurning`, sumindo no último 1,5 s |
| `KeypadLock.Unlocked` / `WrongCode` | `teclado_certo`, `teclado_errado` |
| `Substation.Powered` | `subestacao_liga` |
| `UpgradeTerminal.Chosen` | `ui_upgrade` |
| Segurando E (`PlayerInteractor.IsChanneling`) | Loop `hd_ejetando` (volume 0,3); `disjuntor_reparo` (4 s), `armario_forca` (1 s) , `caixa_abre` (caixa de ferramentas do `FlarePickup`, 0,6 s) e `subestacao_reparo` (6 s, descargas a cada 1,5 s junto do ruído do console) tocam uma vez, do tamanho do segurar E. Param ao soltar, andar ou com o progresso pausado |
| `NeuralModule.Applied` | `modulo_neural` (encaixe, choque e zumbido subindo) |
| Painel da senha: número digitado ou apagado (`KeypadLock.Entry`) | `teclado_tecla` (3 tomadas) |
| Terminal da Eclusa: segurando E (3 s) / `ActivatedEvent` | `eclusa_insere` (os três HDs entrando, com bipes subindo) / `eclusa_liga` (trava pesada e console ligando) |
| Vitória de uma ala (checkpoint salvo) | `ui_salvo` |
| Vitória final (VOCÊ ESCAPOU) | `vitoria_eclusa` (trava, ar despressurizando e o jingle de homologação Ré–Fá#–Lá–Ré), depois a música do laudo |
| Menu, pausa (inclusive ao abrir), configurações e tela de fim (teclado e mouse) | `ui_navegar`, `ui_confirmar`, `ui_voltar` |

O aviso de bateria fraca (`bateria_fraca`) virou um piscar de filamento discreto (antes era um zumbido quadrado de 180 Hz que soava como erro). O EMP (`emp_disparo`) foi refeito: carga de 0,2 s, clique do gatilho, estrondo grave, rajada de estática, zap descendo e cauda de faíscas. A lanterna também mudou: liga e desliga viraram clique mecânico (4 tomadas) e o zumbido ficou um chiado abafado, com volume 0,06 no `AudioConfig`.

Respirações: a do Espécime ficou com o rosnado mais leve e uma pausa no fim do ciclo (6 s), com volume 0,4/0,75 no `AudioConfig`; a do Thomas ofegante (`Player/respiracao_ofegante`, gravação CC0 "scared breathing" da Owlish Media, 15 s em loop) é usada pelo `SanityHeartbeat` quando a cena não tem clip próprio: tom até 1,08x e volume 0,08 a 0,35 no `SanityConfig`. Na cena de morte (sanidade travada pela `MonsterKillScene`) e no fim da partida a respiração corta seco e o `GameSfxHooks` silencia os loops (monstro, Sonâmbulo, câmeras, lanterna, segurar E): fica só o som de morte ou de vitória. O sussurro da alucinação sonora (`Sanity/alucinacao_sussurro`, 3 tomadas, sílabas soltas com bandas de voz) substitui o ruído pulsando a 14,8 Hz do `SanityAudioHallucination`, que soava como algo arrastando; volume 0,75 → 0,5.

Morte pelo Espécime (`especime_ataque`, 3,5 s = animação da `MonsterKillScene`): o bote e o rugido sintéticos de antes, mais gravações CC0 (agarrar, três mordidas e um baque no fim, da Owlish Media; grito masculino do Thomas, baixo e abafado, do OpenGameArt). A respiração corta quando o monstro pega.

Ainda não: som da transição CRT (entra depois da #94), eventos aleatórios de ambiente por fase.

## 6. Mix e níveis

| Grupo | Alvo da fonte (normalizar antes de importar) |
|---|---|
| SFX e passos | -12 LUFS (curtos: pico -3 dBFS) |
| Monstros | -10 LUFS (precisam se destacar) |
| Vozes (bipes) | -12 LUFS |
| UI | -14 LUFS |
| Ambiente | -20 LUFS |
| Música | -16 LUFS |
| Teto de todos | -1 dBTP (limitador no render) |

Mix final do jogo perto de -16 LUFS, medido gravando 5 min de jogo. O alvo de console (-23/-24 LUFS, Sony ASWG-R001) ficou baixo demais: em 22/11/2026 foi preciso pôr o notebook em 240% para ouvir. Jogo de navegador em alto-falante de notebook pede -16 a -14 LUFS.

**Alto-falante pequeno:** notebook quase não reproduz abaixo de ~150 Hz, e a trilha é feita de drones graves (o menu tinha 97% da energia abaixo de 150 Hz). O render soma harmônicos do grave entre 150 e 800 Hz (baixo psicoacústico, `bass_harmonics`, padrão 0,6; 1,4 nas faixas mais graves). Conferir sempre a loudness com o grave cortado (`highpass=f=150`): é o que o notebook toca. Ducking: monstro em perseguição abaixa ambiente em 6 dB; voz da Vance abaixa música em 8 dB.

Teste de escuta: jogar 2 min de olhos fechados com o monstro andando; o jogador deve apontar a direção e dizer se está perto ou longe.

## 7. Produção: como fazemos os sons

| Fonte | Para quê | Registro |
|---|---|---|
| **Síntese em código** (`tools/audio/`, Python + numpy) | Drones, bipes, hum elétrico, câmera, sirenes, ruídos de fita, UI | Receita versionada |
| **Foley gravado** pelo grupo (celular, quarto silencioso, 30 cm) | Passos, armário, papel, metal, respiração, vozes processadas do monstro | Autor e data em `docs/audio/fontes.md` |
| **MIDI + soundfont** (`mido` + FluidSynth) | Motivos melódicos (celesta, piano, jingle) | `.mid` versionado em `audio-src/` |
| **CC0 / royalty free** (Freesound CC0, Sonniss GDC) | Texturas difíceis de gravar | Link e licença em `docs/audio/fontes.md` |

Sem geração de áudio por serviço externo: vocalizações dos monstros saem de foley gravado pelo grupo (voz, garganta, respiração) processado nas receitas.

Pipeline (igual à arte: fonte em texto, saída gerada):

1. Receitas em `audio-src/<Categoria>/<nome>.json` (camadas, envelope, filtro, semente, versões `_dry/_muffled/_memory/_far`).
2. Gravações brutas em `audio-src/Raw/` (WAV), cortadas e tratadas por receita.
3. `python3 tools/audio/render_audio.py` gera `Assets/_Project/Audio/<Categoria>/*.wav`, normaliza com `ffmpeg loudnorm` e confere o teto.
4. Builder de Editor cria/atualiza os `SoundEvent` a partir dos arquivos (`nome_01.wav`, `nome_02.wav` viram takes de `nome`).
5. Import: SFX curtos `DecompressOnLoad`, música e ambiente `CompressedInMemory` (recomendação da doc para Web).

Nomes: `Categoria_Objeto_Acao_Variante_NN`, ex.: `Monster_Specimen_Breath_Far_02`.

Ferramentas na máquina: `ffmpeg` (existe). Instalar: `sudo pacman -S --needed python-numpy python-scipy sox fluidsynth soundfont-fluid` (FluidR3_GM, licença MIT). No Windows, o script roda com `pip install numpy scipy` e o ffmpeg no PATH. Para quem gravar ou compor fora do código: Audacity (edição), Surge XT ou Vital (síntese, grátis) e Reaper ou Ardour (DAW).

## 8. Acessibilidade

- Volumes separados de música, efeitos e voz no menu de pausa.
- Pista visual para cada som P1 que decide a vida do jogador: câmera tem LED, Sonâmbulo tem silhueta ao congelar, monstro perto tem vinheta. Nada crítico só por um som baixo.
- Batimento com limite de volume (jogadores reclamam de batimento irritante em outros jogos): nunca acima da voz.

## 9. Etapas

| # | Etapa | Frente | Estimativa |
|---|---|---|---|
| 1 | Guia (este doc) e referências | RC4 | feito |
| 2 | Teste no WebGL e ADR 0005 (áudio no Web) | RC4 | 1 h |
| 3 | `AudioMixer`, `SoundEvent`, `AudioService`, `AudioDirector`, migração dos 6 scripts com áudio | RC4 | 5 h |
| 4 | `tools/audio/render_audio.py`, receitas base e builder dos `SoundEvent` | RC4 | 4 h |
| 5 | Motivos em MIDI e faixas fixas (menu, sala segura, memória, purga, finais) | RC3 + RC4 | 6 h |
| 6 | Stems das 3 fases | RC2 | 5 h |
| 7 | SFX P1 de monstros, câmera, Lux-90 e skill check | RC1 | 5 h |
| 8 | SFX P2/P3 de mundo, itens, ambiente e eventos aleatórios | RC2 | 4 h |
| 9 | Som das 11 cutscenes e bipe do Thomas | RC3 | 3 h |
| 10 | Mix final, teste de olhos fechados, medição de loudness no WebGL | RC4 | 2 h |

Issues (filhas da #96): etapa 2 #100, 3 #101, 4 #102, 5 #103, 6 #104, 7 #105, 8 #106, 9 #107, 10 #108.

Total de cerca de 35 h. Dependências: a 2 trava a 3, e a 3 e a 4 travam o resto. A trilha (5 e 6) pode começar em paralelo, porque só depende do formato de exportação.

## 10. Critério de pronto

1. Toda ação do jogador e dos monstros tem som, com pelo menos 3 takes nos sons repetidos.
2. No teste de olhos fechados, 4 de 5 tentativas acertam a direção do monstro.
3. A purga de 150 s é sentida sem número na tela.
4. Mix entre -18 e -14 LUFS medido no WebGL, sem clipping, audível em notebook a 100% de volume.
5. Origem de todo arquivo de terceiros registrada.
