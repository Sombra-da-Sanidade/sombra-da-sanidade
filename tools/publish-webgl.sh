#!/usr/bin/env bash
# Publica Builds/WebGL no branch gh-pages (GitHub Pages).
# Uso: tools/publish-webgl.sh [pasta-do-build]   (padrão: Builds/WebGL)
# Gere o build antes pelo menu Sombra da Sanidade > Build WebGL.
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
build="$(realpath "${1:-$root/Builds/WebGL}")"
if [[ ! -f "$build/index.html" ]]; then
  echo "Build não encontrado em $build. Gere pelo menu Sombra da Sanidade > Build WebGL." >&2
  exit 1
fi

sha="$(git -C "$root" rev-parse --short HEAD)"
work="$(mktemp -d)"
trap 'git -C "$root" worktree remove --force "$work" 2>/dev/null || true; rm -rf "$work"' EXIT

git -C "$root" fetch -q origin gh-pages 2>/dev/null || true
if git -C "$root" rev-parse -q --verify origin/gh-pages >/dev/null; then
  git -C "$root" worktree add -q -B gh-pages "$work" origin/gh-pages
else
  git -C "$root" worktree add -q --detach "$work"
  git -C "$work" switch -q --orphan gh-pages
fi

# Substitui todo o conteúdo pelo build novo. .nojekyll evita que o Pages ignore a pasta TemplateData.
git -C "$work" rm -rq --ignore-unmatch .
cp -r "$build"/. "$work"/
touch "$work/.nojekyll"
git -C "$work" add -A
if git -C "$work" diff --cached --quiet; then
  echo "Nada mudou no build."
  exit 0
fi
git -C "$work" commit -qm "chore(build): publish WebGL from $sha"

# O gh autenticado serve de credencial só neste push.
git -C "$work" -c credential.helper= -c credential.helper='!gh auth git-credential' push -q origin gh-pages
echo "Publicado. Link: https://sombra-da-sanidade.github.io/sombra-da-sanidade/"
