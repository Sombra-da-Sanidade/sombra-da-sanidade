# Como contribuir

Regras curtas para 5 pessoas mexerem no mesmo projeto Unity sem quebrar o trabalho uma da outra.

## Fluxo

1. Pegue uma issue no [board do projeto](https://github.com/orgs/Sombra-da-Sanidade/projects) e mova para **Fazendo**.
2. Crie a branch a partir da `main` atualizada:
   ```bash
   git switch main && git pull
   git switch -c feat/12-cone-de-luz
   ```
   Formato: `tipo/<numero-da-issue>-descricao-curta`.
3. Commits pequenos (ver abaixo).
4. Abra um PR para `main` com `Closes #12` na descrição. Precisa de **1 aprovação**.
5. Quem aprovou faz o merge com **Squash and merge**. A branch é apagada.

A `main` não tem proteção técnica, então a regra é combinada: **ninguém faz push direto na `main`**, tudo entra por PR.

## Commits (Conventional Commits)

Formato: `tipo(escopo): descrição em imperativo` (máx. 72 caracteres).

| Tipo | Uso |
|---|---|
| `feat` | Funcionalidade nova |
| `fix` | Correção de bug |
| `docs` | Documentação e GDD |
| `chore` | Configuração, pacotes, settings |
| `refactor` | Melhoria sem mudar comportamento |
| `style` | Formatação |
| `test` | Testes |
| `art` / `audio` | Assets de arte e som |

Escopos: `player`, `flashlight`, `monster`, `sanity`, `interaction`, `ui`, `level`, `gdd`, `unity`.

Exemplos:
```
feat(monster): add patrol between nav nodes
fix(flashlight): stop battery drain when switched off
docs(gdd): update difficulty table for energy wing
```

Uma intenção por commit. Se a mensagem precisa de "e", são dois commits.

## Cenas e prefabs (evitar conflito)

Conflito em `.unity` é o maior risco do projeto. Regras:

- **Cada cena pertence a uma frente** (ver [docs/equipe.md](docs/equipe.md)). Só quem pegou a frente edita. Para mudar a cena de outra frente, combine antes no grupo.
- **Teste na Sandbox da sua frente**: `Assets/_Project/Scenes/Sandbox/Frente<N>.unity` (menu **Sombra da Sanidade > Criar Sandbox**). Pode bagunçar à vontade.
- **Monte tudo em prefabs**. A cena da ala só posiciona prefabs. Mudar o comportamento do monstro = editar o prefab do monstro, não a cena.
- **Números ficam em ScriptableObjects** (`Assets/_Project/ScriptableObjects/Config/`). Ajustar balanceamento não deve exigir mexer em cena nem em código.
- Sempre commite o `.meta` junto do arquivo. Nunca apague `.meta` na mão.

### Merge de cenas e prefabs

O `.gitattributes` já marca os arquivos YAML da Unity com `merge=unityyamlmerge`. Cada pessoa configura uma vez o caminho do UnityYAMLMerge da sua instalação:

```bash
# Windows (ajuste o caminho se o Hub estiver em outro disco)
git config --global merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'

# Linux
git config --global merge.unityyamlmerge.driver "$HOME/Unity/Hub/Editor/6000.5.10f1/Editor/Data/Tools/UnityYAMLMerge merge -p %O %B %A %A"

git config --global merge.unityyamlmerge.name "Unity SmartMerge"
```

## Código C#

- Namespace por sistema: `SombraDaSanidade.Player`, `SombraDaSanidade.Flashlight`, `SombraDaSanidade.Monster`, `SombraDaSanidade.Sanity`, `SombraDaSanidade.Interaction`, `SombraDaSanidade.UI`, `SombraDaSanidade.Core`.
- Identificadores em inglês, comentários e tooltips em português.
- Campos do Inspector: `[SerializeField] private`, nunca `public` só para aparecer no Inspector.
- Classes e métodos em `PascalCase`, campos privados em `camelCase`.
- Sistemas conversam por eventos (`event Action<...>`) ou por propriedades públicas somente leitura. Evite `FindObjectOfType` em `Update`.
- Comente o **porquê**, citando a seção do GDD quando o valor vier de lá.
- Scripts de Editor ficam em `Assets/_Project/Scripts/Editor/`.

## Unity

- Versão única: **6000.5.10f1**. Não atualize o projeto sem combinar com o grupo.
- Pacote novo só via PR, explicando o motivo e registrando em `docs/decisoes/`.

## GDD

- `docs/gdd/gdd.md` é a fonte de verdade para o código. Se o comportamento implementado divergir do GDD, atualize o GDD no mesmo PR (ou abra uma issue com a label `gdd`).
- Material de referência do professor fica em `docs/referencias/` só na sua máquina. O índice está em [docs/referencias/README.md](docs/referencias/README.md).
