# Sombra da Sanidade

Survival horror psicológico em 2D top-down feito em Unity para a disciplina de Jogos Digitais (Bacharelado em Sistemas de Informação, UFRPE, 2026.2).

> Thomas Reis, auditor de sistemas, precisa extrair 3 HDs do Complexo EREBUS e fugir pela eclusa. A lanterna protege a mente, mas atrai o Espécime Vazio, um perseguidor cego e imortal.

## Como abrir

1. Instale o **Unity 6000.5.10f1** pelo Unity Hub (módulo WebGL Build Support recomendado).
2. Clone o repositório:
   ```bash
   git clone https://github.com/Sombra-da-Sanidade/sombra-da-sanidade.git
   ```
3. No Unity Hub: **Add > Add project from disk** e selecione a pasta clonada.
4. Configure o merge de cenas (uma vez por máquina): veja [CONTRIBUTING.md](CONTRIBUTING.md#merge-de-cenas-e-prefabs).
5. Abra `Assets/_Project/Scenes/Sandbox/Prototipo.unity`. Se ela ainda não existir, use o menu **Sombra da Sanidade > Gerar cena de protótipo**.

## Controles

| Ação | Teclado / Mouse |
|---|---|
| Mover | W A S D |
| Mirar lanterna | Mouse |
| Ligar / desligar lanterna | F ou clique esquerdo |
| Pulso EMP | Clique direito |

## Documentação

| Documento | Conteúdo |
|---|---|
| [docs/gdd/gdd.md](docs/gdd/gdd.md) | GDD resumido (fonte de verdade para o código) |
| [docs/gdd/GDD-v1.0.pdf](docs/gdd/GDD-v1.0.pdf) | GDD completo entregue |
| [docs/gdd/gdd-v1.1-propostas.md](docs/gdd/gdd-v1.1-propostas.md) | Propostas para responder ao feedback do professor |
| [docs/equipe.md](docs/equipe.md) | As 5 frentes de trabalho e como escolher uma |
| [docs/mini-demo.md](docs/mini-demo.md) | Plano da mini demo (29/10) |
| [docs/monstro.md](docs/monstro.md) | Espécime Vazio: estados, regras e contratos da IA |
| [docs/arte/guia-de-estilo.md](docs/arte/guia-de-estilo.md) | Direção de arte: paleta, escala, regras de desenho, fichas de personagem |
| [docs/decisoes/](docs/decisoes/) | Decisões técnicas registradas |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Fluxo de trabalho, branches, commits e cenas |

## Estrutura

```
Assets/_Project/
  Scripts/      Core, Player, Flashlight, Monster, Sanity, Interaction, UI, Editor
  ScriptableObjects/Config/   valores de balanceamento (GDD seção 6)
  Scenes/       Main, Wings (alas), Sandbox (testes por frente)
  Prefabs/  Art/  Audio/      Art/ tem PNG gerado de art-src/ (não editar à mão)
art-src/        fonte da pixel art em texto (JSON) e paleta
tools/art/      export_sprites.py: art-src/ -> Assets/_Project/Art/
docs/           GDD, decisões, equipe, arte, índice de referências
```

## Equipe

Marley Martins, Davi Vieira, Luis Filipe, Rony Elias de Oliveira, Thiago Brito.
