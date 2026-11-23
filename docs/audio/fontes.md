# Fontes de áudio

Origem de todo som que não sai só de uma receita sintética (#102, sound-design.md seção 7). Uma linha por arquivo de `audio-src/Raw/` ou por som de terceiros.

| Arquivo | Origem | Autor | Data | Licença / link |
|---|---|---|---|---|
| `Assets/_Project/Audio/Music/menu_drone.wav` | `tools/audio/gen_menu_music.py` (sintético) | grupo | 2026 | própria |
| Trilha `Assets/_Project/Audio/Music/*.ogg` | Receitas `audio-src/Music/` (sintético + MIDI) | grupo | 22/11/2026 | própria |
| Sons de gameplay em `Assets/_Project/Audio/{Player,Lux,Monster,SkillCheck,Camera}/` | Receitas sintéticas em `audio-src/` | grupo | 22/11/2026 | própria |
| Instrumentos das camadas `midi` | Soundfont FluidR3_GM | Frank Wen | | MIT |
| `audio-src/Raw/Kenney/metalClick.ogg`, `metalLatch.ogg`, `creak2.ogg`, `creak3.ogg`, `handleSmallLeather.ogg` | Pacote "RPG Audio" (https://kenney.nl/assets/rpg-audio) | Kenney Vleugels (Kenney.nl) | baixado em 22/11/2026 | CC0 1.0 (http://creativecommons.org/publicdomain/zero/1.0/) |
| `audio-src/Raw/Kenney/impactTin_medium_000.ogg` a `_004.ogg`, `impactMetal_light_000.ogg`, `impactMetal_medium_000.ogg`, `impactGlass_light_001.ogg` | Pacote "Impact Sounds" (https://kenney.nl/assets/impact-sounds) | Kenney Vleugels (Kenney.nl) | baixado em 22/11/2026 | CC0 1.0 (http://creativecommons.org/publicdomain/zero/1.0/) |
| Sons de mundo e interface em `Assets/_Project/Audio/{World,UI}/` | Receitas em `audio-src/World/` e `audio-src/UI/` (sintético; armário, caixa de ferramentas, injeção, disjuntor e HD misturam as amostras do Kenney acima) | grupo | 22/11/2026 | própria + CC0 |
| `audio-src/Raw/OwlishMedia/respiracao_assustada.flac` (convertido para mono de `Human/scared-breathing.wav`) | Pacote "Owlish Media Sound Effects" (https://opengameart.org/content/sound-effects-pack) | Owlish Media (OpenGameArt) | baixado em 22/11/2026 | CC0 1.0 (http://creativecommons.org/publicdomain/zero/1.0/) |
| `audio-src/Raw/OwlishMedia/agarrar.flac` (1 s), `esmagar_1..3.flac`, `pancada.flac` (mono de `Impacts/shouldergrab`, `Impacts/fruit1..3`, `Impacts/hit`) | Pacote "Owlish Media Sound Effects" (https://opengameart.org/content/sound-effects-pack) | Owlish Media (OpenGameArt) | baixado em 22/11/2026 | CC0 1.0 (http://creativecommons.org/publicdomain/zero/1.0/) |
| `audio-src/Raw/OpenGameArt/grito_masculino.flac` (trecho de 20,3 s a 22,0 s, mono, de `death pain grunts.wav`) | "Grunts - Male Death and Pain" (https://opengameart.org/content/grunts-male-death-and-pain) | thebardofblasphemy (OpenGameArt) | baixado em 22/11/2026 | CC0 1.0 (http://creativecommons.org/publicdomain/zero/1.0/) |

### Gravações da música por fase (#104)

| Arquivo | Origem | Autor | Data | Licença / link |
|---|---|---|---|---|
| `audio-src/Raw/Kenney/impactMetal_heavy_000.ogg`, `impactMetal_heavy_002.ogg`, `impactBell_heavy_000.ogg`, `impactPlate_heavy_000.ogg` | Impact Sounds | Kenney | 22/11/2026 | CC0, https://kenney.nl/assets/impact-sounds |
| `audio-src/Raw/OwlishMedia/relogio.flac` | `Ambience/loopable-ticking-clock.wav` (mono) | Owlish Media | 22/11/2026 | CC0, https://opengameart.org/content/sound-effects-pack |
| `audio-src/Raw/OwlishMedia/raspar_1.flac` a `raspar_3.flac` | `Impacts/scrape1..3.wav` (mono) | Owlish Media | 22/11/2026 | CC0, mesmo pacote |
| `audio-src/Raw/OwlishMedia/tambor.flac` | `Impacts/djembe1.wav` (mono) | Owlish Media | 22/11/2026 | CC0, mesmo pacote |
| `audio-src/Raw/OwlishMedia/terremoto.flac` | `Ambience/earthquake.wav`, trecho de 5 s a 57 s, mono 22 kHz | Owlish Media | 22/11/2026 | CC0, mesmo pacote |
