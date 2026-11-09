# God of War II — port nativo para PC · fork estável (feito com IA)

**Fork do [port do otavix010](https://github.com/otavix010/god-of-war-2-pc) com as cutscenes funcionando, o jogo passando
do Colosso, cartão de memória, menos travadas, AMD FSR 3.1 e um instalador de um clique.**

[![Release](https://img.shields.io/github/v/release/niw-maker/god-of-war-2-pc?label=vers%C3%A3o)](https://github.com/niw-maker/god-of-war-2-pc/releases/latest)
![Base](https://img.shields.io/badge/base-0.1.3%20do%20otavix010-blue)
![Feito com IA](https://img.shields.io/badge/feito%20com-IA%20(Claude)-8A2BE2)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![Estado](https://img.shields.io/badge/estado-beta%20(jogo%20inteiro%20n%C3%A3o%20testado)-orange)

> **Tudo neste fork foi feito com IA.** O código, os testes, este texto, o próprio fork no GitHub, o instalador e a issue
> enviada ao criador foram escritos pelo **Claude** (modelo de IA da Anthropic), conduzido por um usuário (`niw-maker`) que
> pediu as melhorias e jogou para conferir.
>
> **O jogo inteiro ainda não foi testado.** Os testes foram automatizados pela IA (roteiros de botões, medição de tempo de
> cada quadro, gravação e análise de vídeo e de som) na abertura, na primeira área e em 7 saves espalhados pelo jogo, mais
> as partidas do usuário (que passou do Colosso e da segunda luta). **Talvez dê para zerar, mas ninguém jogou do começo ao
> fim.** Espere bugs e relate o que encontrar.
>
> **Você precisa da sua própria ISO** do *God of War II* (EUA, SCUS-97481). Nada do jogo vem neste repositório, e nenhuma
> ISO deve ser enviada a ele.

[English below](#god-of-war-ii--native-pc-port--stable-fork-ai-made)

![antes e agora](docs/comparacao.gif)

*Em cima, o port original 0.1.3 como foi baixado; embaixo, este fork, na mesma área, com o medidor de FPS (F10). Jogado
pelo usuário, sem som, na mesma máquina. O GIF tem 50 quadros por segundo (o máximo do formato); para ver a fluidez
completa, baixe o [vídeo de 25 s em 60 fps](docs/comparacao.mp4). A gravação da tela foi feita a 60 fps, então os quadros
gerados acima disso não aparecem nela.*

## Instalar (jeito fácil)

1. Baixe o instalador `GoW2-fork-IA-setup-*.exe` em [Releases](https://github.com/niw-maker/god-of-war-2-pc/releases/latest)
   e instale (pasta padrão `C:\GoW2`; ele cria atalhos no Menu Iniciar e na Área de Trabalho).
   O instalador não tem assinatura digital: se o Windows avisar, clique em *Mais informações → Executar assim mesmo*.
2. Abra o **God of War II** pelo atalho. Na aba **Instalação**, clique em **"Instalar tudo e gerar o jogo"** e escolha a
   **sua** ISO. Uma janela instala o que faltar (Python, compilador C++ da Microsoft, Vulkan SDK) e gera o jogo: espere ela
   dizer **PRONTO** (de 1 a 2 horas na primeira vez; se o Windows pedir permissão, clique em Sim).
3. Clique em **JOGAR**. Em "Frame generation", deixe "AMD FSR 3.1 (recomendado)". **F10** mostra o medidor de FPS.

Precisa de Windows 10/11 de 64 bits com winget, processador com AVX2, 16 GB de RAM e uns 35 GB livres. O caminho completo
(instalador → dependências → jogo gerado) foi testado num Windows limpo (Windows Sandbox).

**Sem o instalador:** com o código deste repositório numa pasta sem espaços, clique duas vezes em `launcher\compilar.cmd`
e abra `GoW2 Launcher.exe`, ou rode `instalar_tudo.ps1 -Iso <sua ISO>`. Sem o launcher: `gerar.cmd` e `jogar.cmd`, como no
original. Sem o Vulkan SDK, o jogo compila igual, só que sem o FSR 3.1.

## Antes e agora

| | 0.1.3 original (base) | Este fork |
|---|---|---|
| Cutscenes em vídeo | tela preta / travavam; **não dava para passar do Colosso** | tocam com imagem, som e legenda; o usuário passou do Colosso e da segunda luta |
| Primeiro chefe | o jogo travava (função fora do mapa) | passa |
| Salvar | o jogo dizia que não havia cartão de memória | salva e carrega no cartão; data do save correta |
| Imagem | faixa verde/roxa embaixo, tracinhos verdes, partículas em "cata-vento" | corrigidas |
| Som | adiantava ~8,5% em combate; tom agudo no lugar do som de alguns vídeos | no tempo certo |
| Primeira área, combate (i7-10700 + RTX 3060, 1080p) | **12-19 fps** | **34-49 fps reais, ~70-90 na tela** com o FSR 3.1 |
| Saves com pouca luta (7 testados) | 47-60 fps | 58-60 fps |
| Travadas em combate (pausas acima de 100 ms por minuto) | 40-115 (antes destes ajustes) | 0-1,4 |

As medidas de fps foram feitas com a compilação **clang-cl + PGO** do PC de testes. O gerador deste repositório compila
com o **MSVC**, que nos 7 saves deu média de 46,6-56,5 fps contra 54,5-59,8 com PGO. As duas ainda não foram comparadas na
primeira área.

**E a versão atual do criador (0.1.6, motor fechado)?** Ela corrigiu, por conta própria, o cartão de memória, a thread de
desenho pelo launcher e a faixa verde. Não tem o FSR 3.1 nem as outras correções de fluidez deste fork. Num teste rápido do
usuário, na mesma máquina, ficou em 30-40 fps com quedas para 17. Como o motor dela é distribuído compilado, as mudanças
deste fork não se aplicam a ela diretamente; por isso este fork parte da 0.1.3, a última versão com o código aberto.

## Todas as mudanças

### Dá para avançar no jogo
- **Cutscenes em vídeo voltaram.** Na 0.1.3 elas ficavam em tela preta e o jogo não passava do Colosso. Eram cinco defeitos
  no decodificador de vídeo (MPEG) e no som (SPU2):
  - o jogo cria o decodificador duas vezes por filme, e a segunda apagava o cabeçalho do vídeo (a causa da tela preta);
  - os tipos de áudio estavam trocados (PCM/ADPCM), então o som não chegava ao jogo, que desistia do vídeo depois de ~4 s;
  - o aviso de "fim do vídeo" não era zerado, e o vídeo seguinte nascia terminado;
  - um tom agudo de 1.714 Hz saía no lugar do som do filme;
  - o jogo travava ao pular vídeos em sequência (menu Tesouros > Cenas do Jogo).
  As legendas em português ficam dentro dos próprios vídeos, então elas voltaram junto.
- **Travamento no primeiro chefe:** o jogo pulava para uma função que o mapa de funções não tinha (`0x1495D0`). Foram
  acrescentadas 293 entradas que faltavam no mapa (`jogo/entradas_extra.txt`).
- **Proteção no fim dos vídeos** (QTE do Colosso): se o jogo ficar esperando o fim de um vídeo que já acabou, o port o
  libera (`jogo/overrides/gow2_claude.cpp`).

### Salvar
- **Cartão de memória funcionando** (`PS2X_MC_SIO2=1`): a 0.1.3 dizia que não havia cartão. O cartão é emulado como no
  PS2, no formato `.ps2` do PCSX2, depois de corrigir dois defeitos do processador de som e entrada (IOP): cargas
  desalinhadas seguidas (`LWL`/`LWR`) liam um valor antigo, e os comandos do cartão iam em blocos de DMA errados.
- **Data dos saves correta:** a leitura do relógio (`sceCdReadClock`) não era tratada ("Invalid Date") e agora entrega a
  hora do Japão, como o console espera (antes o save aparecia 12 horas atrasado).
- **F5 salva em qualquer ponto** (ferramenta de desenvolvedor, `jogar_dev.cmd` ou opção no launcher): abre o menu de
  salvar do próprio jogo, num cartão separado.

### Imagem
- **AMD FSR 3.1 (geração de quadros):** o jogo continua desenhando em OpenGL, e a imagem final passa por uma apresentação
  em Vulkan, onde o FSR 3.1 (FidelityFX SDK 2.4, DLLs assinadas da AMD, baixadas na instalação) gera um quadro entre cada
  dois. Cortes de câmera recomeçam a interpolação; vídeos passam sem geração.
- **Geração de quadros própria** (opção sem Vulkan): shaders que estimam o movimento entre dois quadros ou só misturam os
  dois.
- **Faixa verde/roxa embaixo, tracinhos verdes a cada 64 px e "fantasma" do brilho:** os sprites eram amostrados meio pixel
  adiantados, e o efeito de brilho lia uma linha de fora do quadro.
- **Partículas em "cata-vento":** um valor de textura (o `Q` do GIF) era aplicado na hora em vez de no vértice seguinte.
- **Resolução interna 2× (opcional):** uma passada extra só da geometria, composta como detalhe sobre a imagem. É pesada
  (24-28 fps), por isso vem desligada.

### Fluidez
- **Um quadro em cada quatro se perdia** entre o jogo e a janela: a janela capturava no ritmo do monitor, fora de fase com o
  jogo. Agora todo quadro chega à tela, com ou sem o FSR.
- **Pausas da tela de 70 a 400 ms** vindas da janela (um registro de depuração que travava um mutex do GS e a gravação do
  registro de quadros no disco) foram eliminadas.
- **A janela não espera mais o GS:** com a placa de vídeo carregada, a tela caía para 10-20 atualizações por segundo.
- **Thread de desenho do GS pelo launcher:** o `ambiente.txt` era lido depois de o GS decidir se usava a thread própria.
- **Menos travadas em combate:** efeitos que leem o próprio quadro (distorção das lâminas, desfoque do brilho) faziam até
  ~25 mil cópias por intervalo; agora desenhos que leem a mesma textura compartilham a cópia. A espera do FINISH do GS
  ficou adaptativa (desligada só nas áreas pesadas), e o pré-processamento do GS (`PS2X_GS_PRE`) foi desligado.

### Som
- **Som no tempo certo:** o relógio do IOP adiantava ~8,5% em combate; agora fica preso ao tempo real.
- **Volume dos vídeos** segue o volume de música do jogo, e **Start pula** os vídeos (a abertura também).

### Launcher e instalação
- **Instalador** com atalhos no Menu Iniciar e na Área de Trabalho, e o botão **"Instalar tudo e gerar o jogo"**.
- **Launcher** (`launcher\`): opções de imagem (escalonador, nitidez, FXAA, resolução 2×, geração de quadros),
  predefinições, cartões de memória com nome, backup dos saves e **diagnóstico completo** (registro, tempo de cada quadro,
  vídeo da janela e opções usadas, em `out\diagnosticos`).
- **Medidor (F10)** com os quadros gerados: "FPS x + y gerados = z na tela".

### Para desenvolvedores
- Tudo o que muda no runtime está em `patches/zz_claude_gs_audio.patch`, sobre o PS2Recomp fixado.
- Porta de controle para testes automáticos (`PS2X_CONTROLE=<porta>`: ler e escrever a memória, apertar botões, tirar
  fotos) e gravação de vídeo da janela (`PS2X_GRAVA`).

## Problemas conhecidos
- O jogo inteiro não foi testado.
- A ampliação temporal do FSR 3.1 não foi feita: precisa de vetores de movimento, e a câmera da memória do jogo não explica
  um balanço de 6-7 px por quadro (investigação em andamento).
- Nas áreas mais pesadas, ~35-45 fps reais num i7-10700: a emulação do VU1 é o gargalo.
- O erro de som `989 ERROR 55` apareceu uma vez ao carregar um save e não se repetiu; o aviso "controle removido" apareceu
  uma vez ao ligar o DualSense com o jogo aberto.

## Créditos
- Port original: [otavix010](https://github.com/otavix010/god-of-war-2-pc).
- Ideias e endereços da espera do FINISH: [cxldsakeb](https://github.com/cxldsakeb) (GoW2-nativo-PS2, GPL-3.0).
- [PS2Recomp](https://github.com/ran-j/PS2Recomp) (ran-j); [AMD FidelityFX SDK](https://github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK)
  (licença MIT; as DLLs são baixadas pelo gerador, não vêm no repositório).
- *God of War* é marca da Sony Interactive Entertainment; este projeto de fãs não tem relação com ela.

---

# God of War II — native PC port · stable fork (AI-made)

**A fork of [otavix010's port](https://github.com/otavix010/god-of-war-2-pc) with working cutscenes, the game getting past
the Colossus, memory card saves, fewer stutters, AMD FSR 3.1 and a one-click installer.**

> **Everything in this fork was made with AI.** The code, the tests, this text, the GitHub fork itself, the installer and the
> issue sent to the original author were written by **Claude** (Anthropic's AI model), directed by a user (`niw-maker`) who
> asked for the improvements and played to check them.
>
> **The whole game has not been tested yet.** Testing was automated by the AI (button scripts, per-frame timing, video and
> audio analysis) on the intro, the first area and 7 saves across the game, plus the user's play sessions (past the Colossus
> and the second fight). **It may be finishable, but nobody has played it start to finish.** Expect bugs.
>
> **You need your own ISO** of *God of War II* (USA, SCUS-97481). No game data is in this repository.

**Install:** download `GoW2-fork-IA-setup-*.exe` from [Releases](https://github.com/niw-maker/god-of-war-2-pc/releases/latest),
install it (default `C:\GoW2`, Start menu and desktop shortcuts), open it, Install tab → "Instalar tudo e gerar o jogo" and
pick **your** ISO; one window installs what's missing (Python, C++ build tools, Vulkan SDK) and generates the game. Then
PLAY. The full path was tested on a clean Windows (Windows Sandbox).

**What changed over 0.1.3:**
- **Playable further:** video cutscenes were black and the game couldn't get past the Colossus. Five MPEG/SPU2 defects
  fixed (the second `sceMpegCreate` per movie wiped the sequence header; PCM/ADPCM stream types swapped; end flag not
  cleared; a 1,714 Hz tone instead of the movie audio; a hang when skipping movies). A first-boss hang (function missing
  from the map at `0x1495D0`) fixed with 293 map entries. A safety release for end-of-movie waits.
- **Saving:** the memory card now works (emulated over SIO2; two IOP bugs fixed: back-to-back `LWL`/`LWR` load delay and
  SIO2 DMA block layout); `sceCdReadClock` handled, in Japan time as the console expects (correct save dates); F5 dev save.
- **Image:** AMD FSR 3.1 frame generation through a Vulkan presenter on top of the OpenGL renderer; an alternative
  shader-based frame generation; sprite sampling half a pixel off (green/purple band, green dashes, bloom ghost); GIF `Q`
  applied too early (pinwheel particles); optional 2× internal resolution.
- **Smoothness:** one frame in four lost between game and window; 70-400 ms window stalls; the window no longer waits for
  the GS; the launcher lost the GS drawing thread (`ambiente.txt` read too late); feedback-texture batching (up to ~25k
  copies per interval), adaptive FINISH wait, `PS2X_GS_PRE=0`.
- **Sound:** IOP clock tied to real time (it ran ~8.5% fast in combat); FMV volume follows the game; Start skips FMVs.
- **Launcher and installer:** one-click install, image options and presets, named memory cards, save backup, full
  diagnostics, FPS meter with generated frames.

**Measured** (i7-10700 + RTX 3060, 1080p): first-area combat 12-19 fps on 0.1.3 → 34-49 real fps, ~70-90 on screen with
FSR 3.1; light saves 47-60 → 58-60; combat stalls over 100 ms 40-115 → 0-1.4 per minute. Measured with a clang-cl + PGO build; the
generator here builds with MSVC (7-save average 46.6-56.5 vs 54.5-59.8 with PGO). The author's current 0.1.6 (closed
prebuilt engine) ran at 30-40 fps with drops to 17 in a quick test on the same PC.

**Known issues:** whole game untested; no FSR 3.1 temporal upscaling yet (motion vectors); heaviest areas ~35-45 real fps
(VU1 emulation bound); one-off `989 ERROR 55` and one "controller removed" message.

**Credits:** otavix010 (original port), cxldsakeb (FINISH-wait idea and addresses, GoW2-nativo-PS2, GPL-3.0), ran-j
(PS2Recomp), AMD (FidelityFX SDK, MIT). *God of War* is a trademark of Sony Interactive Entertainment; this fan project is
not affiliated with it.

---

*Abaixo, o README original da versão 0.1.3 do criador / original 0.1.3 README by the author:*

# God of War II — port nativo para PC

> **Projeto de fãs, sem fins lucrativos.** *God of War II* e *God of War* são marcas da Sony Interactive Entertainment; o jogo foi desenvolvido pelo Santa Monica Studio. Este projeto não tem relação com a Sony Interactive Entertainment. Este repositório **não contém o jogo**: você precisa da **sua própria cópia** do DVD (a versão americana, **SCUS-97481**).

[English summary below](#english-summary)

## O que é

Um port **nativo** do *God of War II* (PlayStation 2, 2007) para Windows. Não é um emulador: o código do jogo é **traduzido para C++ e compilado** para x86-64 (recompilação estática), em cima do [PS2Recomp](https://github.com/ran-j/PS2Recomp).

Como o código do jogo não pode ser distribuído, **o seu PC gera e compila a parte do jogo a partir da sua ISO**. Um comando só faz tudo.

## Estado atual: beta

O jogo roda do começo, com som, vídeos e gráficos como no PlayStation 2. **Roda a 60 FPS em PCs atuais** (testado num Ryzen 7 9800X3D). Num notebook com processador de 4 núcleos (i5 de 8ª geração), use o **modo 30 FPS**, que segura o ritmo estável. A parte gráfica do console (GS) roda na placa de vídeo (OpenGL 4.3). Os microprogramas de geometria do VU1 são traduzidos para C++ na sua máquina, a partir do executável do seu disco (a etapa "VU1" do gerador).

Problemas conhecidos: em alguns PCs o vídeo de abertura pode travar de vez em quando (feche e abra de novo); uma faixa verde fina aparece nas últimas linhas da imagem em algumas telas. Relate o que encontrar pelo launcher.

## Opções

Pelo [Otavix Launcher](https://github.com/otavix010/otavix-launcher/releases/latest), no menu de opções do jogo: **30 ou 60 FPS**, janela ou **tela cheia** (no jogo, F11 ou Alt+Enter alternam), tamanho da janela, escalonador (**bilinear, AMD FSR 1 ou NVIDIA Image Scaling**) e a nitidez dele, anti-serrilhamento **FXAA**, proporção 4:3, 16:9, 21:9 e 32:9 (ultrawide) e predefinições (Notebook / PC fraco, Equilibrado, Qualidade). Sem o launcher, as mesmas opções são variáveis de ambiente: `PS2X_QPS=30`, `PS2X_TELA_CHEIA=1`, `PS2X_JANELA_TAM=1920x1080`, `PS2X_ESCALA=nenhum|bilinear|fsr|nis`, `PS2X_NITIDEZ_PCT=0..100`, `PS2X_FXAA=1`, `PS2X_ASPECTO=4:3|16:9|21:9|32:9|tela|nativo`.

## Como gerar

### Pelo Otavix Launcher

O [Otavix Launcher](https://github.com/otavix010/otavix-launcher/releases/latest) lista este port: ele baixa o port, pede a ISO, gera o jogo e abre.

### À mão

**1. O que você precisa ter:** a sua cópia do *God of War II* **americano** (SCUS-97481, "God of War II (USA)") como imagem **.iso** do DVD. O gerador confere o executável do jogo pelo hash: outras versões não servem, porque o mapa de funções é o da americana.

**2. Instale as ferramentas (uma vez só)**, num terminal:
```
winget install Python.Python.3.12
winget install Microsoft.VisualStudio.2022.BuildTools --override "--quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.CMake.Project --add Microsoft.VisualStudio.Component.VC.Llvm.Clang --includeRecommended"
```
O resto o gerador baixa sozinho: o PS2Recomp (na versão fixada), as bibliotecas dele e, se o PC não tiver Git, o Git portátil (MinGit).

**3. Baixe este repositório** (botão *Code > Download ZIP*, ou `git clone`) e extraia numa pasta **sem espaços no caminho**, por exemplo `C:\god-of-war-2-pc`. Deixe uns 25 GB livres nesse disco (os arquivos do disco, o código traduzido e a compilação).

**4. Clique duas vezes em `gerar.cmd`** e escolha a ISO. Ele:
- confere a ISO e extrai os arquivos do disco para `trabalho\disc`;
- baixa o PS2Recomp, aplica as nossas correções (`patches\`) e compila o tradutor;
- traduz o código do jogo e os microprogramas do VU1 para C++ (só no seu PC);
- compila o jogo (de 30 minutos a 1 hora e meia, conforme o PC; precisa de 16 GB de RAM ou mais e de um processador com AVX2).

**5. Jogue com `jogar.cmd`.** O jogo lê os arquivos de `trabalho\disc` e a ISO no lugar onde ela estava na geração (se mudar a ISO de lugar, rode `gerar.cmd -RefazerDe 2`).

Depois de atualizar o port, `gerar.cmd -RefazerDe 3` aplica as correções novas, retraduz e recompila sem extrair o disco de novo. Com o componente "C++ Clang tools" do Visual Studio instalado (já incluído no comando acima), a geometria compila com o clang-cl e fica mais rápida.

## Como funciona

- **PS2Recomp** traduz o executável do PS2 (MIPS R5900, o "Emotion Engine") para C++ e compila com o runtime dele.
- **O mapa de funções** (`jogo\ghidra_funcoes.csv`) veio de uma análise com o [Ghidra](https://ghidra-sre.org/) e a extensão [ghidra-emotionengine-reloaded](https://github.com/chaoticgd/ghidra-emotionengine-reloaded): são só endereços e nomes, nada do código do jogo. `jogo\entradas_extra.txt` lista funções chamadas só por ponteiro e alvos de tabelas de salto que a análise não marcou.
- **A receita do VU1** (`jogo\vu1_receita.json`) diz onde os microprogramas de geometria ficam no executável (offsets e tamanhos, conferidos por SHA-1) e como montar a memória de código do VU1; o único trecho que não vem do executável é uma instrução de 8 bytes que o próprio jogo monta enquanto roda.
- **As nossas correções no PS2Recomp** (GPLv3 como ele), em `patches\`: no tradutor (desvios e ponto flutuante como no EE) e no runtime (o "sistema operacional" do console, leitura de setores direto da ISO, o chip de som SPU2 emulado no IOP, vídeos MPEG, o GS desenhado na placa de vídeo, o VU1 traduzido, threads por núcleo e o menu de imagem: FSR 1, NIS e FXAA).
- O modo 30 FPS usa o limitador do próprio jogo (um remendo de memória no endereço 2D80B8, como os que os jogadores usam no PCSX2), aplicado pelo runtime quando `PS2X_QPS=30`.

## Licença e créditos

- **O nosso código** (`gerar.ps1`, `gerar.cmd`, `ferramentas\`): [MIT](LICENSE).
- **PS2Recomp** (baixado pelo gerador) e **as nossas correções nele** (`patches\`): [GPLv3](https://github.com/ran-j/PS2Recomp/blob/main/LICENSE), de ran-j e colaboradores. O executável gerado no seu PC também é GPLv3.
- O NIS vem do [NVIDIA Image Scaling SDK](https://github.com/NVIDIAGameWorks/NVIDIAImageScaling) (MIT) e o FSR 1 do [AMD FidelityFX FSR](https://github.com/GPUOpen-Effects/FidelityFX-FSR) (MIT).
- *God of War II* e *God of War* são marcas da Sony Interactive Entertainment. Este é um projeto de fãs, sem fins lucrativos, sem relação com a Sony Interactive Entertainment nem o Santa Monica Studio nem endossado por eles.

## English summary

A **native PC port** of *God of War II* (PlayStation 2, 2007) made by **static recompilation** with [PS2Recomp](https://github.com/ran-j/PS2Recomp). **Beta.** It runs from the start with sound, videos and PS2-accurate graphics, at **60 FPS on current PCs**; on 4-core laptops use the **30 FPS mode**. The console's graphics chip (GS) runs on your GPU (OpenGL 4.3). Options in the launcher: 30/60 FPS, window or fullscreen (F11 / Alt+Enter), window size, upscaler (bilinear, AMD FSR 1, NVIDIA Image Scaling) and sharpness, FXAA, aspect ratio 4:3, 16:9, 21:9 and 32:9 (ultrawide).

**Bring your own game**: no game code or data here, only the function map (addresses), translation settings and the VU1 "recipe" (offsets and sizes in your executable). You need the **US** release (SCUS-97481, Redump "God of War II (USA)") as an **.iso**. Install Python 3.11+ and Visual Studio 2022 Build Tools with C++, CMake and Clang (commands above), download this repository to a path without spaces with about 25 GB free, double-click **`gerar.cmd`** (it checks your ISO, extracts the disc files, fetches the pinned PS2Recomp, applies our patches, translates the game code on your PC and builds it; 16 GB of RAM and an AVX2 CPU needed), then play with **`jogar.cmd`**.

License: our scripts are MIT; PS2Recomp and our patches to it are GPLv3. *God of War II* and *God of War* are trademarks of Sony Interactive Entertainment; this is a non-commercial fan project, not affiliated with or endorsed by Sony Interactive Entertainment or Santa Monica Studio.
