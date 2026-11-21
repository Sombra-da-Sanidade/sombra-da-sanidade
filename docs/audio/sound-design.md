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

### Faixas fixas

| Faixa | Duração | Descrição |
|---|---|---|
| Menu | loop 90 s | Drone atual + contagem invertida na celesta |
| Sala segura | loop 60 s | Única faixa "calma": acordes abertos, fita. Fade nos últimos 10 s da Zona Segura avisa que a luz vai cair |
| Memória | loop 30 s | Contagem com wow/flutter e passa-baixa pré-renderados |
| Eclusa / purga | **150 s exatos** | Composição linear que é o próprio cronômetro: pulso acelera de 60 a 140 bpm, sirene entra em 90 s e 130 s. O jogador sabe quanto falta pelo ouvido |
| Revelação (C3) | 45 s | Contagem descendente pela primeira vez, voz da Vance limpa |
| Final "Testemunha" | 60 s | Contagem resolvida em Ré maior, sem fita |
| Final "Amostra entregue" | 60 s | Jingle Aethelgard limpo e alegre sobre o drone: ironia |
| Game Over | 6 s | Corte seco, zumbido de ouvido, fita parando |
| Laudo | loop 40 s | Impressora matricial + drone suave |

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

## 6. Mix e níveis

| Grupo | Alvo da fonte (normalizar antes de importar) |
|---|---|
| SFX e passos | -18 LUFS (curtos: pico -6 dBFS) |
| Monstros | -16 LUFS (precisam se destacar) |
| Vozes (bipes) | -18 LUFS |
| Ambiente | -28 LUFS |
| Música | -24 LUFS |
| Teto de todos | -1 dBTP |

Mix final do jogo perto de -23 LUFS (Sony ASWG-R001 para PC/console), medido gravando 5 min de jogo. Ducking: monstro em perseguição abaixa ambiente em 6 dB; voz da Vance abaixa música em 8 dB.

Teste de escuta: jogar 2 min de olhos fechados com o monstro andando; o jogador deve apontar a direção e dizer se está perto ou longe.

## 7. Produção: como fazemos os sons

| Fonte | Para quê | Registro |
|---|---|---|
| **Síntese em código** (`tools/audio/`, Python + numpy) | Drones, bipes, hum elétrico, câmera, sirenes, ruídos de fita, UI | Receita versionada |
| **Foley gravado** pelo grupo (celular, quarto silencioso, 30 cm) | Passos, armário, papel, metal, respiração, vozes processadas do monstro | Autor e data em `docs/arte/FONTES` |
| **MIDI + soundfont** (`mido` + FluidSynth) | Motivos melódicos (celesta, piano, jingle) | `.mid` versionado em `audio-src/` |
| **CC0 / royalty free** (Freesound CC0, Sonniss GDC) | Texturas difíceis de gravar | Link e licença em `docs/arte/FONTES` |

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
4. Mix entre -25 e -21 LUFS medido no WebGL, sem clipping.
5. Origem de todo arquivo de terceiros registrada.
