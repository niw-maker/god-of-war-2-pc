// Claude (05/11/2026): ajustes do God of War II (SCUS-97481, ELF SHA-1 e6a62a0f...).
#include <chrono>
#include <cstring>
#include <cstdio>
#include <cstdlib>
#include <thread>
#include "game_overrides.h"
#include "ps2_runtime.h"
#include "ps2_runtime_macros.h"
#include "ps2_recompiled_functions.h"
#include "otx_controle_porta.h"
#include "runtime/ee_scheduler.h"
#include <algorithm>
#include <array>
#include <atomic>

#if defined(_WIN32)
extern "C"
{
    __declspec(dllimport) short __stdcall GetAsyncKeyState(int tecla);
    __declspec(dllimport) void *__stdcall GetForegroundWindow();
    __declspec(dllimport) unsigned long __stdcall GetWindowThreadProcessId(void *janela, unsigned long *processo);
    __declspec(dllimport) unsigned long __stdcall GetCurrentProcessId();
}
#endif

void otxCameraGuarda(const float *cam16, const float *proj16); // Claude (08/11): gs_thread_backend.cpp
extern std::atomic<uint64_t> g_otxTickExibe;
extern std::atomic<int64_t> g_otxTempoExibeNs;
extern std::atomic<uint32_t> g_otxQuadrosExibe; // ps2_runtime.cpp: quadros mostrados (um por quadro do jogo)
extern std::atomic<int> g_otxBaseTrinta;
void otxPedeCameraVu(PS2Runtime *runtime, const float *pos, const float *proj); // Claude (08/11): ps2_runtime.cpp // Claude (08/11): ps2_runtime.cpp (1 = base em 30 com geração de quadros)

namespace
{
    // 0x14E380 entrega o quadro ao GS e espera em 0x18ADD0 o FINISH, ou seja, o tempo do EE e o do VU1 se somam.
    // No PC o VU1 roda em outra thread com uma cópia dos dados do DMA, então o EE pode montar o próximo quadro enquanto
    // isso: o início do quadro seguinte (0x186E08) continua esperando a mesma marca, com no máximo um quadro em voo.
    // 0x14E4F8 e 0x14E670 (boot e telas de carregamento) não entram: lá dois FINISH se juntavam em uma interrupção só.
    // Ideia e endereços: cxldsakeb/GoW2-nativo-PS2 (GPL-3.0).
    // Claude (08/11): medido nos 7 saves e na primeira área: sem a espera, o combate pesado vai de 30,5 para 42,6 fps, mas
    // onde o jogo já faz 60 os engasgos aumentam (save 3: 8,7 -> 30 acima de 50 ms por minuto). Por isso é adaptativo: só
    // deixa de esperar quando, esperando, o jogo fica perto de 30 (média de quadro acima de 24 ms por ~1 s), e volta a
    // esperar quando, sem esperar, ele já faz 60 (média abaixo de 17,5 ms por ~1 s).
    // PS2X_SEM_ESPERA_FINISH=1 nunca espera; =0 sempre espera (como o PS2); sem a variável, adaptativo.
    int g_modoEspera = -1; // -1 adaptativo, 0 sempre espera, 1 nunca espera
    bool g_semEsperaAgora = false;

    // Claude (08/11): geração de quadros adaptativa (com o FSR 3.1). Uma tela de taxa fixa só mostra liso o que divide a
    // taxa dela: 60 reais sem geração e 30 reais + 1 gerado (60 na tela) cabem em 60/120/180/240 Hz; 50 irregulares ou
    // 120 gerados num monitor de 180 Hz tremem. Então: enquanto o jogo faz mais que ~36 fps, nada de geração; quando ele
    // cai para os 30 estáveis da espera do FINISH (áreas pesadas), o FSR completa os 60. A troca tem histerese.
    // PS2X_FSR_MODO=sempre volta à geração sempre ligada com a base livre.
    bool g_fgAdaptativo = false;
    bool g_base30 = false;
    void fgAdaptativo(PS2Runtime *runtime, double msTrabalho, double msQuadro)
    {
        static double mediaQuadro = 16.7, mediaTrabalho = 10.0;
        static int seguidos = 0;
        mediaQuadro = mediaQuadro * 0.9 + std::min(msQuadro, 100.0) * 0.1;
        mediaTrabalho = mediaTrabalho * 0.95 + std::min(msTrabalho, 100.0) * 0.05;
        // (08/11, revisto 2x) a base não é forçada (segurar a thread do jogo atrasa a interrupção de vsync). Medido com o
        // usuário em combate pesado: com a espera do FINISH sempre ligada o jogo caía para 20-28 (quadros de 3 vsyncs); então
        // a espera continua adaptativa (desliga no pesado: EE e GS em paralelo, 35-48 fps) e a geração liga quando a média de
        // quadro passa de 24 ms (base abaixo de ~42 fps, ~0,75 s) e desliga abaixo de 19 ms (acima de ~52 fps, ~1 s).
        (void)mediaTrabalho;
        const bool quer30 = g_base30 ? !(mediaQuadro < 19.0) : (mediaQuadro > 24.0);
        seguidos = quer30 != g_base30 ? seguidos + 1 : 0;
        if (seguidos >= (g_base30 ? 60 : 45))
        {
            g_base30 = quer30;
            seguidos = 0;
            mediaQuadro = quer30 ? 33.3 : 16.7;
            mediaTrabalho = 10.0;
            g_otxBaseTrinta.store(quer30 ? 1 : 0, std::memory_order_relaxed);
            static int logs = 0;
            if (logs++ < 200)
                std::fprintf(stderr, "[gow2-claude] base %s\n", quer30 ? "abaixo de ~42 fps: geração de quadros ligada" : "acima de ~52 fps: geração de quadros desligada");
        }
        (void)runtime;
    }
    void gow2EsperaQuadro(uint8_t *rdram, R5900Context *ctx, PS2Runtime *runtime)
    {
        const uint32_t ra = GPR_U32(ctx, 31);
        if (ra >= 0x0014E380u && ra < 0x0014E4F8u)
        {
            // Claude (08/11): câmera deste quadro para os vetores de movimento do FSR 3.1: [0x2D7BCC]+0xD0 = câmera->mundo,
            // [0x2D7BC4]+0x40 = projeção
            {
                uint32_t pc, pp;
                std::memcpy(&pc, rdram + 0x2D7BCCu, 4);
                std::memcpy(&pp, rdram + 0x2D7BC4u, 4);
                pc &= 0x1FFFFFFu;
                pp &= 0x1FFFFFFu;
                if (pc >= 0x100000u && pc < 0x1FF0000u && pp >= 0x100000u && pp < 0x1FF0000u)
                {
                    otxCameraGuarda(reinterpret_cast<const float *>(rdram + pc + 0xD0u), reinterpret_cast<const float *>(rdram + pp + 0x40u));
                    // PS2X_CAMERA=fila: a câmera também vai pela fila dos desenhos até o GS (alinhada com a imagem)
                    static const bool naFila = [] { const char *e = std::getenv("PS2X_CAMERA"); return e && std::strcmp(e, "fila") == 0; }();
                    static const bool doVu = [] { const char *e = std::getenv("PS2X_CAMERA"); return e && std::strcmp(e, "vu") == 0; }();
                    if (doVu)
                    {
                        // pede à thread gráfica (em ordem) que ache a câmera do próximo quadro na memória do VU1
                        float pos[3], proj[16];
                        std::memcpy(pos, rdram + pc + 0xD0u + 48u, sizeof pos);
                        std::memcpy(proj, rdram + pp + 0x40u, sizeof proj);
                        otxPedeCameraVu(runtime, pos, proj);
                    }
                    if (naFila)
                    {
                        std::array<float, 32> cam;
                        std::memcpy(cam.data(), rdram + pc + 0xD0u, 16 * sizeof(float));
                        std::memcpy(cam.data() + 16, rdram + pp + 0x40u, 16 * sizeof(float));
                        runtime->memory().otxNaFilaExibicao([runtime, cam]() { runtime->gs().otxPostaCamera(cam.data()); });
                    }
                }
            }
            if (g_fgAdaptativo)
            {
                // geração de quadros adaptativa: o quadro é o intervalo entre trocas de quadro mostradas (registradas onde
                // o jogo grava o buffer de exibição, ps2_runtime.cpp); vale com e sem a espera do FINISH
                static int64_t tTrocaAnt = 0;
                const int64_t tTroca = g_otxTempoExibeNs.load(std::memory_order_relaxed);
                if (tTroca != 0 && tTroca != tTrocaAnt)
                {
                    const double msQuadro = tTrocaAnt ? (tTroca - tTrocaAnt) / 1e6 : 16.7;
                    fgAdaptativo(runtime, 0.0, msQuadro);
                }
                tTrocaAnt = tTroca;
            }
            if (g_modoEspera == -1)
            {
                static auto ultimo = std::chrono::steady_clock::now();
                static double media = 16.7;
                static int seguidos = 0;
                const auto agora = std::chrono::steady_clock::now();
                const double ms = std::chrono::duration<double, std::milli>(agora - ultimo).count();
                ultimo = agora;
                if (ms < 200.0)
                    media = media * 0.9 + ms * 0.1;
                const bool quer = g_semEsperaAgora ? !(media < 17.5) : (media > 24.0);
                seguidos = quer != g_semEsperaAgora ? seguidos + 1 : 0;
                if (seguidos >= 45)
                {
                    g_semEsperaAgora = quer;
                    seguidos = 0;
                    media = 16.7 + (quer ? 8.0 : 0.0);
                    static int logs = 0;
                    if (logs++ < 40)
                        std::fprintf(stderr, "[gow2-claude] espera do quadro %s\n", quer ? "desligada (jogo pesado)" : "ligada (jogo leve)");
                }
            }
            if (g_modoEspera == 1 || (g_modoEspera == -1 && g_semEsperaAgora))
            {
                ctx->pc = ra;
                return;
            }
        }
        {
            // diagnóstico (PS2X_LOG_FPS): chamadas por faixa de endereço de retorno e estado da geração adaptativa
            static const bool logA = std::getenv("PS2X_LOG_FPS") != nullptr;
            static auto t0 = std::chrono::steady_clock::now();
            static uint32_t nDentro = 0u, nFora = 0u, raFora = 0u;
            if (ra >= 0x0014E380u && ra < 0x0014E4F8u)
                ++nDentro;
            else
            {
                ++nFora;
                raFora = ra;
            }
            if (logA && std::chrono::steady_clock::now() - t0 > std::chrono::seconds(5))
            {
                t0 = std::chrono::steady_clock::now();
                std::fprintf(stderr, "[gow2-claude] finish: dentro=%u fora=%u (ra=0x%x) adaptativo=%d base30=%d exibe=%u\n", nDentro, nFora, raFora,
                             g_fgAdaptativo ? 1 : 0, g_base30 ? 1 : 0, g_otxQuadrosExibe.load(std::memory_order_relaxed));
                nDentro = nFora = 0u;
            }
        }
        FUN_0018add0_0x18add0(rdram, ctx, runtime);
    }

    // No fim de um vídeo, 0x1910A8 repete 0x2A8F80 até a fila de exibição da libmpeg ([0x33E840]) esvaziar. Depois do QTE do
    // Colosso o jogo ficou preso nesse laço. Passados 4 s sem esvaziar (no normal leva uma fração de segundo), esvazia e
    // segue. PS2X_TRAVA_VIDEO=0 desliga.
    void gow2FimDoVideo(uint8_t *rdram, R5900Context *ctx, PS2Runtime *runtime)
    {
        static bool esperando = false;
        static std::chrono::steady_clock::time_point desde;
        FUN_002a8f80_0x2a8f80(rdram, ctx, runtime);
        if (GPR_U32(ctx, 2) != 0u)
        {
            esperando = false;
            return;
        }
        const auto agora = std::chrono::steady_clock::now();
        if (!esperando)
        {
            esperando = true;
            desde = agora;
            return;
        }
        if (agora - desde > std::chrono::seconds(4))
        {
            std::fprintf(stderr, "[gow2-claude] fim de video preso: fila=%u estado=%u\n",
                         *reinterpret_cast<uint32_t *>(rdram + 0x33E840u), *reinterpret_cast<uint32_t *>(rdram + 0x33E718u));
            *reinterpret_cast<uint32_t *>(rdram + 0x33E840u) = 0u;
            SET_GPR_U32(ctx, 2, 1u);
            esperando = false;
        }
    }

    // Ferramenta de desenvolvedor, desligada no jogo normal: PS2X_DEV_SAVE=<modo> faz F5 pedir o salvamento em qualquer ponto.
    // O script dos pontos de salvar grava o modo em 0x2D84C0 e o laço do jogo (0x1DDA74) o entrega a 0x26BB28 no quadro seguinte.
    // PS2X_DEV_SAVE_SEG=<s> faz o mesmo pedido sozinho depois de s segundos (para testes automáticos).
    void ligaDevSave(PS2Runtime &runtime)
    {
        const char *e = std::getenv("PS2X_DEV_SAVE");
        if (!(e && *e && e[0] != '0'))
            return;
        const uint32_t modo = static_cast<uint32_t>(std::strtoul(e, nullptr, 0));
        const char *es = std::getenv("PS2X_DEV_SAVE_SEG");
        const double seg = es ? std::atof(es) : 0.0;
        uint8_t *rdram = runtime.memory().getRDRAM();
        std::fprintf(stderr, "[dev-save] ligado: F5 pede o salvamento (modo %u)\n", modo);
        std::thread([rdram, modo, seg]
        {
            const auto inicio = std::chrono::steady_clock::now();
            bool automatico = seg > 0.0, antes = false;
            while (true)
            {
                std::this_thread::sleep_for(std::chrono::milliseconds(30));
                bool pede = false;
                if (automatico && std::chrono::duration<double>(std::chrono::steady_clock::now() - inicio).count() >= seg)
                {
                    automatico = false;
                    pede = true;
                }
#if defined(_WIN32)
                unsigned long dono = 0;
                GetWindowThreadProcessId(GetForegroundWindow(), &dono);
                const bool f5 = dono == GetCurrentProcessId() && (GetAsyncKeyState(0x74) & 0x8000) != 0;
                pede = pede || (f5 && !antes);
                antes = f5;
#endif
                if (pede)
                {
                    *reinterpret_cast<volatile uint32_t *>(rdram + 0x2D84C0u) = modo;
                    std::fprintf(stderr, "[dev-save] pedido enviado\n");
                }
            }
        }).detach();
    }

    // Claude (08/11): Start pula os vídeos. O player (0x1910A8) termina o vídeo quando o campo +340 do filme ([0x337700]) não é
    // zero, o mesmo caminho do fim normal. Só age com o player tocando (estado 11 em 0x2D8190). PS2X_PULA_VIDEO=0 desliga.
    void ligaPulaVideo(PS2Runtime &runtime)
    {
        const char *e = std::getenv("PS2X_PULA_VIDEO");
        if (e && e[0] == '0')
            return;
        uint8_t *rdram = runtime.memory().getRDRAM();
        std::thread([rdram]
        {
            bool antes = false;
            for (;;)
            {
                std::this_thread::sleep_for(std::chrono::milliseconds(20));
                const bool start = otxPadStartApertado();
                if (start && !antes)
                {
                    const uint32_t estado = *reinterpret_cast<volatile uint32_t *>(rdram + 0x2D8190u);
                    const uint32_t filme = *reinterpret_cast<volatile uint32_t *>(rdram + 0x337700u) & 0x1FFFFFFu;
                    if (estado == 11u && filme >= 0x100000u && filme + 344u < 0x2000000u)
                    {
                        *reinterpret_cast<volatile uint32_t *>(rdram + filme + 340u) = 1u;
                        std::fprintf(stderr, "[gow2-claude] vídeo pulado com Start\n");
                    }
                }
                antes = start;
            }
        }).detach();
    }

    void aplica(PS2Runtime &runtime)
    {
        ligaDevSave(runtime);
        ligaPulaVideo(runtime);
        const char *e = std::getenv("PS2X_SEM_ESPERA_FINISH");
        g_modoEspera = !e ? -1 : (e[0] == '1' ? 1 : 0);
        // Claude (08/11): com a geração de quadros do FSR, a base precisa ser regular: nas áreas pesadas o jogo fica em 30
        // estáveis (o FSR mostra 60) em vez de 35-48 irregulares, que viravam tremida
        {
            const char *fg = std::getenv("PS2X_FSR_FG"), *vk = std::getenv("PS2X_VULKAN");
            if (fg && fg[0] == '1' && vk && vk[0] == '1')
            {
                // (08/11) a espera do FINISH segue adaptativa (ou o que PS2X_SEM_ESPERA_FINISH pedir)
                // (08/11) a pedido do usuário a geração fica sempre ligada (o atraso é pequeno e cai com mais fps);
                // PS2X_FSR_MODO=adaptativo volta a ligar só abaixo de ~42 fps
                const char *modo = std::getenv("PS2X_FSR_MODO");
                g_fgAdaptativo = modo && std::strcmp(modo, "adaptativo") == 0;
                if (!g_fgAdaptativo)
                    g_otxBaseTrinta.store(1, std::memory_order_relaxed);
            }
        }
        // o gancho fica sempre instalado: com a espera "sempre" ele chama a espera original, mas ainda guarda a câmera e
        // conduz a geração de quadros adaptativa (antes ele só era instalado fora desse modo, e a adaptativa nunca rodava)
        std::fprintf(stderr, "[gow2-claude] espera do quadro %s, geração adaptativa %s: %d\n",
                     g_modoEspera == 1 ? "desligada" : (g_modoEspera == 0 ? "sempre" : "adaptativa"), g_fgAdaptativo ? "ligada" : "desligada",
                     runtime.replaceFunction(0x0018ADD0u, gow2EsperaQuadro) ? 1 : 0);
        const char *v = std::getenv("PS2X_TRAVA_VIDEO");
        if (!(v && v[0] == '0'))
            runtime.replaceFunction(0x002A8F80u, gow2FimDoVideo);
    }
}

PS2_REGISTER_GAME_OVERRIDE("gow2-claude", "SCUS_974.81", 0u, 0u, aplica)
