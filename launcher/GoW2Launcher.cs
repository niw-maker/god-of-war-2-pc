// Launcher do God of War II nativo: guarda as opções em launcher_config.ini e abre o jogo com elas.
// Compilar: launcher\compilar.cmd. Sem instalação; usa o .NET Framework que já vem no Windows.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("God of War II - Launcher")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

static class Programa
{
    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var janela = new Janela();
        // --foto <arquivo>: grava uma imagem da própria janela; --jogar: abre o jogo direto (testes)
        if (args.Length >= 2 && args[0] == "--foto")
            return janela.Foto(args[1]);
        if (args.Length >= 1 && args[0] == "--jogar")
            return janela.JogarSemJanela();
        Application.Run(janela);
        return 0;
    }
}

sealed class Opcoes
{
    public string Modo = "tela", Tamanho = "1920x1080", Proporcao = "16:9", Escala = "fsr", Fps = "60", Fg = "0", Cartao = "colosso";
    public int Nitidez = 50;
    public bool Fxaa = true, Res2x = false, Medidor = false, Vibracao = true, SemSom = false, DevSave = false, PulaVideo = true, Diagnostico = false;

    public void Le(string arq)
    {
        string[] linhas;
        try { linhas = File.Exists(arq) ? File.ReadAllLines(arq) : new string[0]; }
        catch (Exception) { return; } // arquivo preso por outro programa: abre com o padrão
        foreach (var linha in linhas)
        {
            int i = linha.IndexOf('=');
            if (i <= 0) continue;
            string k = linha.Substring(0, i).Trim(), v = linha.Substring(i + 1).Trim();
            switch (k)
            {
                case "modo": Modo = v; break;
                case "tamanho": Tamanho = v; break;
                case "proporcao": Proporcao = v; break;
                case "escala": Escala = v; break;
                case "fps": Fps = v; break;
                case "fg": Fg = v; break;
                case "cartao": Cartao = v; break;
                case "nitidez": int n; if (int.TryParse(v, out n)) Nitidez = Math.Max(0, Math.Min(100, n)); break;
                case "fxaa": Fxaa = v == "1"; break;
                case "res2x": Res2x = v == "1"; break;
                case "medidor": Medidor = v == "1"; break;
                case "vibracao": Vibracao = v == "1"; break;
                case "semsom": SemSom = v == "1"; break;
                case "devsave": DevSave = v == "1"; break;
                case "pulavideo": PulaVideo = v == "1"; break;
                case "diagnostico": Diagnostico = v == "1"; break;
            }
        }
    }

    public void Grava(string arq)
    {
        Func<bool, string> b = x => x ? "1" : "0";
        File.WriteAllLines(arq, new[] {
            "modo=" + Modo, "tamanho=" + Tamanho, "proporcao=" + Proporcao, "escala=" + Escala, "fps=" + Fps, "fg=" + Fg,
            "cartao=" + Cartao, "nitidez=" + Nitidez, "fxaa=" + b(Fxaa), "res2x=" + b(Res2x), "medidor=" + b(Medidor),
            "vibracao=" + b(Vibracao), "semsom=" + b(SemSom), "devsave=" + b(DevSave), "pulavideo=" + b(PulaVideo), "diagnostico=" + b(Diagnostico) });
    }

    // as variáveis que o runtime lê (conferidas no código dele); o ambiente.txt do bin não passa por cima delas
    public Dictionary<string, string> Ambiente(string raiz)
    {
        Func<bool, string> b = x => x ? "1" : "0";
        var d = new Dictionary<string, string> {
            {"PS2X_TELA_CHEIA", b(Modo == "tela")}, {"PS2X_JANELA_TAM", Tamanho}, {"PS2X_ASPECTO", Proporcao},
            {"PS2X_ESCALA", Escala}, {"PS2X_NITIDEZ_PCT", Nitidez.ToString()}, {"PS2X_FXAA", b(Fxaa)}, {"PS2X_QPS", Fps},
            {"PS2X_GS_HI", b(Res2x)}, {"PS2X_FG", Fg == "fsr" ? "0" : Fg}, {"PS2X_OSD", b(Medidor)}, {"PS2X_VIBRACAO", b(Vibracao)}, {"PS2X_MUDO", b(SemSom)}, {"PS2X_PULA_VIDEO", b(PulaVideo)} };
        // FSR 3.1 da AMD: a imagem passa pelo Vulkan e o swapchain do FSR gera um quadro entre cada dois do jogo
        d["PS2X_VULKAN"] = b(Fg == "fsr");
        d["PS2X_FSR_FG"] = b(Fg == "fsr");
        if (DevSave)
        {
            string dir = Path.Combine(raiz, @"out\cartoes_dev");
            Directory.CreateDirectory(dir);
            d["PS2X_DEV_SAVE"] = "1";
            d["PS2X_MC_SIO2_ARQ"] = Path.Combine(dir, NomeCartao(Cartao) + ".ps2");
        }
        return d;
    }

    // só letras e números ASCII (o jogo abre o arquivo pela página de código do Windows) e nada de CON, NUL, COM1...
    public static string NomeCartao(string nome)
    {
        var s = new string((nome ?? "").Where(c => (c < 128 && char.IsLetterOrDigit(c)) || c == '-' || c == '_').ToArray());
        if (s.Length == 0) return "dev";
        string m = s.ToUpperInvariant();
        if (m == "CON" || m == "PRN" || m == "AUX" || m == "NUL" || ((m.StartsWith("COM") || m.StartsWith("LPT")) && m.Length == 4 && char.IsDigit(m[3])))
            s = "cartao_" + s;
        return s;
    }
}

sealed class Janela : Form
{
    static readonly Color Fundo = Color.FromArgb(24, 22, 22), Painel = Color.FromArgb(36, 32, 32), Texto = Color.FromArgb(235, 228, 220),
        Apagado = Color.FromArgb(165, 155, 145), Vermelho = Color.FromArgb(170, 32, 28);

    readonly string raiz = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    readonly Opcoes op = new Opcoes();
    string Config { get { return Path.Combine(raiz, "launcher_config.ini"); } }

    ComboBox cModo, cTamanho, cProporcao, cEscala, cFps, cFg;
    TrackBar tNitidez;
    Label lNitidez, lEstado;
    CheckBox kFxaa, k2x, kMedidor, kVibracao, kSemSom, kDev, kPula, kDiag;
    TextBox tCartao;
    Button bJogar;
    bool carregando;

    static readonly string[][] Modos = { new[] {"tela", "Tela cheia"}, new[] {"janela", "Janela"} };
    static readonly string[][] Proporcoes = { new[] {"16:9", "16:9 (widescreen)"}, new[] {"tela", "Igual ao monitor"}, new[] {"4:3", "4:3 (original)"}, new[] {"21:9", "21:9 (ultrawide)"}, new[] {"32:9", "32:9 (super ultrawide)"} };
    static readonly string[][] Escalas = { new[] {"fsr", "AMD FSR 1 (recomendado)"}, new[] {"nis", "NVIDIA Image Scaling"}, new[] {"bilinear", "Bilinear"}, new[] {"nenhum", "Nenhum"} };
    static readonly string[][] Taxas = { new[] {"60", "60 FPS"}, new[] {"30", "30 FPS (mais estável em PC fraco)"} };
    static readonly string[][] Geradores = { new[] {"0", "Desligado"}, new[] {"fsr", "AMD FSR 3.1 (recomendado)"}, new[] {"1", "Experimental: por movimento"}, new[] {"2", "Experimental: mistura"} };
    static readonly string[][] Tamanhos = { new[] {"1280x720", "1280 × 720"}, new[] {"1600x900", "1600 × 900"}, new[] {"1920x1080", "1920 × 1080"}, new[] {"2560x1440", "2560 × 1440"}, new[] {"3840x2160", "3840 × 2160"} };
    readonly Dictionary<ComboBox, List<string>> valores = new Dictionary<ComboBox, List<string>>();

    public Janela()
    {
        op.Le(Config);
        Text = "God of War II - Launcher";
        ClientSize = new Size(620, 640);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Fundo;
        ForeColor = Texto;
        Font = new Font("Segoe UI", 9.5f);
        try { Icon = Icon.ExtractAssociatedIcon(Path.Combine(raiz, @"bin\god-of-war-2.exe")); } catch { }

        Controls.Add(new Label { Text = "GOD OF WAR II", Font = new Font("Georgia", 22f, FontStyle.Bold), ForeColor = Color.FromArgb(205, 60, 50), Location = new Point(20, 12), AutoSize = true });
        Controls.Add(new Label { Text = "Port nativo. Passe o mouse sobre uma opção para ver o que ela faz.", ForeColor = Apagado, Location = new Point(23, 52), AutoSize = true });

        var abas = new TabControl { Location = new Point(20, 80), Size = new Size(580, 440) };
        abas.TabPages.Add(AbaImagem());
        abas.TabPages.Add(AbaDesempenho());
        abas.TabPages.Add(AbaCartao());
        abas.TabPages.Add(AbaAjuda());
        abas.TabPages.Add(AbaInstalacao());
        // sem o jogo gerado, abre direto na aba de instalação
        if (!File.Exists(Path.Combine(raiz, @"bin\god-of-war-2.exe"))) abas.SelectedIndex = abas.TabPages.Count - 1;
        Controls.Add(abas);

        lEstado = new Label { Location = new Point(23, 535), Size = new Size(575, 40), ForeColor = Apagado };
        Controls.Add(lEstado);
        bJogar = new Button { Text = "JOGAR", Location = new Point(400, 580), Size = new Size(200, 44), BackColor = Vermelho, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12f, FontStyle.Bold) };
        bJogar.FlatAppearance.BorderSize = 0;
        bJogar.Click += (s, e) => Jogar();
        Controls.Add(bJogar);
        var bPadrao = Botao("Restaurar padrão", new Point(20, 586), 150);
        bPadrao.Click += (s, e) => { Copia(new Opcoes()); Mostra(); Grava(); };
        Controls.Add(bPadrao);

        Mostra();
        Atualiza();
        AcceptButton = bJogar;
    }

    Button Botao(string texto, Point p, int largura)
    {
        var b = new Button { Text = texto, Location = p, Size = new Size(largura, 32), BackColor = Painel, ForeColor = Texto, FlatStyle = FlatStyle.Flat };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 80, 75);
        return b;
    }

    readonly ToolTip dica = new ToolTip { AutoPopDelay = 15000, InitialDelay = 300 };

    TabPage Aba(string titulo) { return new TabPage(titulo) { BackColor = Painel, ForeColor = Texto }; }

    ComboBox Lista(TabPage a, string rotulo, int y, string[][] itens, string explica)
    {
        var l = new Label { Text = rotulo, Location = new Point(16, y + 4), Size = new Size(170, 22) };
        var c = new ComboBox { Location = new Point(190, y), Width = 355, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Fundo, ForeColor = Texto, FlatStyle = FlatStyle.Flat };
        foreach (var i in itens) c.Items.Add(i[1]);
        valores[c] = itens.Select(i => i[0]).ToList();
        c.SelectedIndexChanged += (s, e) => Mudou();
        dica.SetToolTip(l, explica); dica.SetToolTip(c, explica);
        a.Controls.Add(l); a.Controls.Add(c);
        return c;
    }

    CheckBox Marca(TabPage a, string rotulo, int y, string explica)
    {
        var k = new CheckBox { Text = rotulo, Location = new Point(16, y), Size = new Size(530, 24) };
        k.CheckedChanged += (s, e) => Mudou();
        dica.SetToolTip(k, explica);
        a.Controls.Add(k);
        return k;
    }

    void Nota(TabPage a, string texto, int y, int altura)
    {
        a.Controls.Add(new Label { Text = texto, Location = new Point(16, y), Size = new Size(535, altura), ForeColor = Apagado });
    }

    TabPage AbaImagem()
    {
        var a = Aba("Imagem");
        cModo = Lista(a, "Exibição", 16, Modos, "Durante o jogo, F11 ou Alt+Enter alternam entre janela e tela cheia.");
        cTamanho = Lista(a, "Tamanho da janela", 52, Tamanhos, "Só vale no modo janela.");
        cProporcao = Lista(a, "Proporção", 88, Proporcoes, "O jogo original é 4:3. Nas outras, a câmera do jogo é alargada.");
        cEscala = Lista(a, "Ampliação", 124, Escalas, "Como a imagem é ampliada até o tamanho da tela.");
        var ln = new Label { Text = "Nitidez", Location = new Point(16, 164), Size = new Size(170, 22) };
        tNitidez = new TrackBar { Location = new Point(182, 158), Width = 320, Minimum = 0, Maximum = 100, TickFrequency = 10, BackColor = Painel };
        lNitidez = new Label { Location = new Point(506, 164), Size = new Size(45, 22) };
        tNitidez.ValueChanged += (s, e) => { lNitidez.Text = tNitidez.Value + "%"; Mudou(); };
        dica.SetToolTip(tNitidez, "Nitidez do FSR ou do NVIDIA Image Scaling.");
        a.Controls.Add(ln); a.Controls.Add(tNitidez); a.Controls.Add(lNitidez);
        kFxaa = Marca(a, "Anti-serrilhamento FXAA", 208, "Suaviza as bordas serrilhadas, com custo baixo.");
        k2x = Marca(a, "Resolução interna 2× (experimental, muito pesado)", 240, "Desenha a geometria em 1024×896. Bem mais nítido, mas no RTX 3060 o trecho medido caiu de 60 para cerca de 25 FPS.");
        Nota(a, "A resolução 2× não aumenta o HUD nem os efeitos de tela, que são só ampliados. Os vídeos pré-renderizados não mudam.", 272, 44);
        var p = new Label { Text = "Predefinições", Location = new Point(16, 330), AutoSize = true };
        a.Controls.Add(p);
        var b1 = Botao("Desempenho", new Point(16, 356), 165);
        var b2 = Botao("Equilibrado", new Point(195, 356), 165);
        var b3 = Botao("Qualidade (pesado)", new Point(374, 356), 165);
        b1.Click += (s, e) => Predefine("nenhum", 0, false, false, "0");
        b2.Click += (s, e) => Predefine("fsr", 50, true, false, "0");
        b3.Click += (s, e) => Predefine("fsr", 50, true, true, "0");
        dica.SetToolTip(b3, "Resolução 2× com FSR e FXAA. Pesado: no RTX 3060 o trecho medido caiu de 60 para cerca de 25 FPS.");
        a.Controls.Add(b1); a.Controls.Add(b2); a.Controls.Add(b3);
        return a;
    }

    TabPage AbaDesempenho()
    {
        var a = Aba("Desempenho");
        cFps = Lista(a, "Taxa do jogo", 16, Taxas, "Limite da lógica do jogo. Em 30, o ritmo fica mais estável em PCs fracos.");
        cFg = Lista(a, "Frame generation", 52, Geradores, "FSR 3.1 (adaptativo): onde o jogo cai para 30 (áreas pesadas), a AMD gera um quadro entre cada dois e você vê 60 lisos; onde o jogo já faz 60, nada é gerado (sem atraso nem artefatos). Os experimentais criam quadros só quando o jogo fica abaixo de 60.");
        Nota(a, "Como funciona: com o jogo perto de 60 FPS, o gerador não faz nada e não acrescenta atraso. Abaixo disso, ele mostra quadros intermediários até chegar a 60 na tela, atrasando a imagem em um quadro real (cerca de 33 ms a 30 FPS).\r\n\r\n" +
            "Por movimento: mantém grades e bordas nítidas, mas objetos muito rápidos podem se fragmentar por um instante.\r\n" +
            "Mistura: sem fragmentos, mas objetos rápidos ficam com imagem dupla.\r\n\r\n" +
            "Com a resolução 2× ligada, o gerador quase não age: a tela não passa de cerca de 40 atualizações por segundo.", 90, 175);
        kMedidor = Marca(a, "Abrir com o medidor de FPS ligado", 280, "F10 liga e desliga durante o jogo. Mostra FPS, quadros gerados, CPU, GPU, temperatura, VRAM e RAM.");
        kVibracao = Marca(a, "Vibração do controle", 312, "Liga ou desliga a vibração.");
        kSemSom = Marca(a, "Sem som", 344, "Abre o jogo mudo.");
        kPula = Marca(a, "Start pula as cutscenes em vídeo", 376, "Com um vídeo tocando, Start encerra o vídeo e o jogo segue. As cenas feitas com o próprio motor do jogo não são afetadas.");
        return a;
    }

    TabPage AbaCartao()
    {
        var a = Aba("Saves");
        Nota(a, "O cartão de memória normal fica em trabalho\\disc\\mc0. Faça um backup antes de testar coisas novas.", 14, 40);
        var bBackup = Botao("Fazer backup dos saves", new Point(16, 58), 200);
        bBackup.Click += (s, e) => Backup();
        var bPasta = Botao("Abrir pasta de backups", new Point(226, 58), 200);
        bPasta.Click += (s, e) => { string d = Path.Combine(raiz, @"out\backups_cartao"); Directory.CreateDirectory(d); Process.Start("explorer.exe", "\"" + d + "\""); };
        a.Controls.Add(bBackup); a.Controls.Add(bPasta);
        a.Controls.Add(new Label { Text = "Ferramenta de desenvolvedor", Font = new Font(Font, FontStyle.Bold), Location = new Point(16, 116), AutoSize = true });
        kDev = Marca(a, "F5 abre o menu de salvar em qualquer ponto", 144, "Usa o salvamento do próprio jogo. Não é um save state: inimigos e eventos da área voltam como o jogo define.");
        var lc = new Label { Text = "Cartão separado", Location = new Point(16, 182), Size = new Size(170, 22) };
        tCartao = new TextBox { Location = new Point(190, 180), Width = 200, BackColor = Fundo, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle };
        tCartao.TextChanged += (s, e) => Mudou();
        dica.SetToolTip(tCartao, "Cada nome é um cartão próprio em out\\cartoes_dev. O cartão normal não é tocado com esta opção ligada.");
        a.Controls.Add(lc); a.Controls.Add(tCartao);
        Nota(a, "Com um cartão novo, em \"Criar novo arquivo\", o cursor começa em Não: suba para Sim antes de confirmar.", 218, 44);
        return a;
    }

    TabPage AbaAjuda()
    {
        var a = Aba("Ajuda");
        Nota(a, "Teclas durante o jogo\r\n\r\n  F10: medidor de FPS\r\n  F11 ou Alt+Enter: janela / tela cheia\r\n  F5: menu de salvar (só com a ferramenta de desenvolvedor)\r\n\r\n" +
            "Se o jogo fechar sozinho ou travar, o botão abaixo abre o registro da última sessão.", 14, 170);
        var b = Botao("Abrir registro da última sessão", new Point(16, 192), 260);
        b.Click += (s, e) => { string l = Path.Combine(raiz, @"out\jogo.log"); if (File.Exists(l)) Process.Start("notepad.exe", "\"" + l + "\""); else Avisa("Ainda não há registro."); };
        a.Controls.Add(b);
        kDiag = Marca(a, "Guardar diagnóstico completo (registro, tempos de quadro e vídeo da tela)", 236,
            "Cada partida ganha uma pasta em out\\diagnosticos com o registro do jogo, o tempo de cada quadro e um vídeo da janela (precisa do ffmpeg instalado; a placa de vídeo grava, com custo baixo). Serve para analisar travadas e erros depois.");
        var bd = Botao("Abrir pasta de diagnósticos", new Point(16, 270), 260);
        bd.Click += (s, e) => { string d = Path.Combine(raiz, @"out\diagnosticos"); Directory.CreateDirectory(d); Process.Start("explorer.exe", "\"" + d + "\""); };
        a.Controls.Add(bd);
        return a;
    }

    // Claude (08/11): instalação pelo launcher. As dependências vêm pelo winget (o instalador de programas do Windows); o jogo é
    // gerado da ISO do próprio jogador pelo gerar.cmd, numa janela que mostra o andamento
    CheckBox kFfmpegDep;

    TabPage AbaInstalacao()
    {
        var a = Aba("Instalação");
        Nota(a, "1. Instale as dependências (uma vez só). Abre uma janela do Windows que instala, pelo winget: Python 3.12, " +
            "Visual Studio 2022 Build Tools (C++, CMake e Clang) e Vulkan SDK (para o AMD FSR 3.1). Pode pedir permissão de administrador " +
            "e levar de 10 a 30 minutos. Depois, feche e abra o launcher.", 14, 76);
        var bDep = Botao("Instalar dependências", new Point(16, 94), 260);
        bDep.Click += (s, e) => InstalaDependencias();
        a.Controls.Add(bDep);
        kFfmpegDep = new CheckBox { Text = "Também o ffmpeg (vídeo do modo diagnóstico)", Location = new Point(290, 98), Size = new Size(270, 24) };
        a.Controls.Add(kFfmpegDep);
        Nota(a, "2. Gere o jogo a partir da SUA ISO do God of War II (EUA, SCUS-97481). Nada do jogo vem com este projeto. " +
            "A pasta do projeto precisa estar num caminho sem espaços e ter uns 25 GB livres; a geração leva de 30 minutos a 1 hora " +
            "(16 GB de RAM e processador com AVX2). Para atualizar depois de baixar uma versão nova, use \"Gerar de novo\".", 140, 76);
        var bGera = Botao("Escolher a ISO e gerar o jogo", new Point(16, 220), 260);
        bGera.Click += (s, e) => GeraJogo(0);
        a.Controls.Add(bGera);
        var bRegera = Botao("Gerar de novo (sem extrair o disco)", new Point(290, 220), 255);
        bRegera.Click += (s, e) => GeraJogo(3);
        dica.SetToolTip(bRegera, "Aplica as correções novas, retraduz e recompila (gerar.cmd -RefazerDe 3).");
        a.Controls.Add(bRegera);
        Nota(a, "Tudo neste port foi feito com IA (Claude). O jogo inteiro ainda não foi testado do começo ao fim: só testes " +
            "automatizados e partidas de quem usa. Relate o que encontrar.", 270, 50);
        return a;
    }

    void InstalaDependencias()
    {
        if (!Pergunta("Instalar as dependências pelo winget? Uma janela vai mostrar o andamento.")) return;
        var k = kFfmpegDep;
        var cmds = new List<string> {
            "winget install --id Python.Python.3.12 -e --accept-source-agreements --accept-package-agreements",
            "winget install --id Microsoft.VisualStudio.2022.BuildTools -e --accept-source-agreements --accept-package-agreements --override " +
                "'--quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.CMake.Project " +
                "--add Microsoft.VisualStudio.Component.VC.Llvm.Clang --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --includeRecommended'",
            "winget install --id KhronosGroup.VulkanSDK -e --accept-source-agreements --accept-package-agreements" };
        if (k != null && k.Checked) cmds.Add("winget install --id Gyan.FFmpeg -e --accept-source-agreements --accept-package-agreements");
        string script = "Write-Host 'Instalando as dependencias do God of War II (pode levar de 10 a 30 minutos)' -ForegroundColor Cyan; " +
            string.Join("; ", cmds) + "; Write-Host ''; Write-Host 'Pronto. Feche esta janela e abra o launcher de novo.' -ForegroundColor Green";
        try { Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NoExit -ExecutionPolicy Bypass -Command \"" + script + "\"") { UseShellExecute = true }); }
        catch (Exception e) { Avisa("Não consegui abrir o instalador: " + e.Message + "\r\n\r\nO winget vem com o Windows 10/11 (\"Instalador de Aplicativo\" da Microsoft Store)."); }
    }

    void GeraJogo(int refazerDe)
    {
        if (raiz.Contains(" ")) { Avisa("Mova a pasta do projeto para um caminho sem espaços (agora: " + raiz + ")."); return; }
        string gerar = Path.Combine(raiz, "gerar.cmd");
        if (!File.Exists(gerar)) { Avisa("Não achei o gerar.cmd ao lado do launcher."); return; }
        string args = "";
        if (refazerDe > 0) args = " -RefazerDe " + refazerDe;
        else
        {
            using (var d = new OpenFileDialog { Title = "Escolha a ISO do God of War II (EUA, SCUS-97481)", Filter = "Imagem de DVD (*.iso)|*.iso|Todos os arquivos (*.*)|*.*" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                args = " -Iso \"" + d.FileName + "\"";
            }
        }
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", "/c \"\"" + gerar + "\"" + args + "\"") { UseShellExecute = true, WorkingDirectory = raiz });
            lEstado.Text = "Gerando o jogo na outra janela. Quando ela disser \"pronto\", feche-a e clique em JOGAR.";
        }
        catch (Exception e) { Avisa("Não consegui iniciar a geração: " + e.Message); }
    }

    bool Pergunta(string texto) { return MessageBox.Show(this, texto, "God of War II", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }

    void Predefine(string escala, int nitidez, bool fxaa, bool res2x, string fg)
    {
        op.Escala = escala; op.Nitidez = nitidez; op.Fxaa = fxaa; op.Res2x = res2x; op.Fg = fg;
        Mostra(); Grava();
    }

    void Copia(Opcoes o)
    {
        op.Modo = o.Modo; op.Tamanho = o.Tamanho; op.Proporcao = o.Proporcao; op.Escala = o.Escala; op.Fps = o.Fps; op.Fg = o.Fg; op.Cartao = o.Cartao;
        op.Nitidez = o.Nitidez; op.Fxaa = o.Fxaa; op.Res2x = o.Res2x; op.Medidor = o.Medidor; op.Vibracao = o.Vibracao; op.SemSom = o.SemSom; op.DevSave = o.DevSave; op.PulaVideo = o.PulaVideo; op.Diagnostico = o.Diagnostico;
    }

    // um valor que não está na lista (editado à mão no .ini) entra como opção própria, em vez de virar o primeiro item
    void Escolhe(ComboBox c, string valor)
    {
        var v = valores[c];
        int i = v.IndexOf(valor);
        if (i < 0 && !string.IsNullOrEmpty(valor))
        {
            v.Add(valor);
            c.Items.Add(valor + " (personalizado)");
            i = v.Count - 1;
        }
        c.SelectedIndex = i < 0 ? 0 : i;
    }
    string Valor(ComboBox c) { return valores[c][Math.Max(0, c.SelectedIndex)]; }

    void Mostra()
    {
        carregando = true;
        Escolhe(cModo, op.Modo);
        Escolhe(cTamanho, op.Tamanho);
        Escolhe(cProporcao, op.Proporcao);
        Escolhe(cEscala, op.Escala);
        Escolhe(cFps, op.Fps);
        Escolhe(cFg, op.Fg);
        tNitidez.Value = op.Nitidez; lNitidez.Text = op.Nitidez + "%";
        kFxaa.Checked = op.Fxaa; k2x.Checked = op.Res2x; kMedidor.Checked = op.Medidor; kVibracao.Checked = op.Vibracao;
        kSemSom.Checked = op.SemSom; kDev.Checked = op.DevSave; kPula.Checked = op.PulaVideo; kDiag.Checked = op.Diagnostico; tCartao.Text = op.Cartao;
        carregando = false;
        Ajusta();
    }

    void Ajusta()
    {
        cTamanho.Enabled = Valor(cModo) == "janela";
        tNitidez.Enabled = Valor(cEscala) == "fsr" || Valor(cEscala) == "nis";
        tCartao.Enabled = kDev.Checked;
    }

    void Mudou()
    {
        if (carregando) return;
        op.Modo = Valor(cModo);
        op.Tamanho = Valor(cTamanho);
        op.Proporcao = Valor(cProporcao);
        op.Escala = Valor(cEscala);
        op.Fps = Valor(cFps);
        op.Fg = Valor(cFg);
        op.Nitidez = tNitidez.Value;
        op.Fxaa = kFxaa.Checked; op.Res2x = k2x.Checked; op.Medidor = kMedidor.Checked; op.Vibracao = kVibracao.Checked;
        op.SemSom = kSemSom.Checked; op.DevSave = kDev.Checked; op.PulaVideo = kPula.Checked; op.Diagnostico = kDiag.Checked; op.Cartao = tCartao.Text;
        Ajusta();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Grava();
        base.OnFormClosing(e);
    }

    void Grava()
    {
        try { op.Grava(Config); }
        catch (Exception e) { lEstado.Text = "Não consegui guardar as opções: " + e.Message; }
    }

    void Avisa(string texto) { MessageBox.Show(this, texto, "God of War II", MessageBoxButtons.OK, MessageBoxIcon.Information); }

    void Backup()
    {
        try
        {
            if (Process.GetProcessesByName("god-of-war-2").Length > 0)
            {
                Avisa("Feche o jogo antes do backup: com ele aberto, o cartão pode estar sendo gravado.");
                return;
            }
            string destino = Path.Combine(raiz, @"out\backups_cartao", DateTime.Now.ToString("yyyy-MM-dd_HHmmss_fff"));
            int n = 0;
            string normal = Path.Combine(raiz, @"trabalho\disc\mc0");
            if (Directory.Exists(normal))
                foreach (var f in Directory.GetFiles(normal, "*.ps2"))
                {
                    Directory.CreateDirectory(destino);
                    File.Copy(f, Path.Combine(destino, Path.GetFileName(f)));
                    ++n;
                }
            string dev = Path.Combine(raiz, @"out\cartoes_dev");
            if (Directory.Exists(dev))
                foreach (var f in Directory.GetFiles(dev, "*.ps2"))
                {
                    Directory.CreateDirectory(destino);
                    File.Copy(f, Path.Combine(destino, "dev_" + Path.GetFileName(f)));
                    ++n;
                }
            lEstado.Text = n == 0 ? "Nenhum cartão encontrado para copiar." : n + " cartão(ões) copiado(s) para " + destino;
        }
        catch (Exception e) { lEstado.Text = "O backup falhou: " + e.Message; }
    }

    // o que precisa existir para abrir o jogo; devolve a mensagem do problema ou null
    string Confere(out string exe, out string iso)
    {
        exe = Path.Combine(raiz, @"bin\god-of-war-2.exe");
        iso = null;
        if (!File.Exists(exe)) return "O jogo ainda não foi gerado (falta bin\\god-of-war-2.exe). Use a aba Instalação.";
        string ref_ = Path.Combine(raiz, @"trabalho\iso.txt");
        if (!File.Exists(ref_)) return "Não achei trabalho\\iso.txt. Gere o jogo pela aba Instalação.";
        iso = File.ReadAllText(ref_, new UTF8Encoding(false)).Trim();
        if (!File.Exists(iso)) return "A ISO não está mais em:\r\n" + iso + "\r\n\r\nDevolva o arquivo a esse lugar ou rode o gerar.cmd de novo.";
        if (Process.GetProcessesByName("god-of-war-2").Length > 0) return "O jogo já está aberto.";
        return null;
    }

    Process Abre(string exe, string iso)
    {
        string disco = Path.Combine(raiz, @"trabalho\disc"), log = Path.Combine(raiz, @"out\jogo.log");
        Directory.CreateDirectory(Path.Combine(raiz, "out"));
        // diagnóstico: uma pasta por partida com o registro, os tempos de quadro, o vídeo da janela e as opções usadas
        string diag = null;
        if (op.Diagnostico)
        {
            diag = Path.Combine(raiz, @"out\diagnosticos\" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(diag);
            log = Path.Combine(diag, "jogo.log");
        }
        // guarda o registro da sessão anterior: é nele que estaria a pista de um travamento
        try { if (File.Exists(log)) File.Copy(log, Path.Combine(raiz, @"out\jogo_anterior.log"), true); } catch (IOException) { }
        var si =new ProcessStartInfo("cmd.exe", "/c \"\"" + exe + "\" \"" + Path.Combine(disco, "SCUS_974.81") + "\" > \"" + log + "\" 2>&1\"")
        {
            UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = disco
        };
        si.EnvironmentVariables["PS2X_CD_IMAGE"] = iso;
        si.EnvironmentVariables["PS2X_CD_ROOT"] = disco;
        // variáveis de desenvolvedor herdadas de um console (jogar_dev) não podem valer com a opção desligada
        si.EnvironmentVariables.Remove("PS2X_DEV_SAVE");
        si.EnvironmentVariables.Remove("PS2X_DEV_SAVE_SEG");
        si.EnvironmentVariables.Remove("PS2X_MC_SIO2_ARQ");
        foreach (var kv in op.Ambiente(raiz)) si.EnvironmentVariables[kv.Key] = kv.Value;
        if (diag != null)
        {
            si.EnvironmentVariables["PS2X_LOG_FPS"] = "1";
            si.EnvironmentVariables["PS2X_LOG_QUADROS"] = Path.Combine(diag, "quadros.txt");
            si.EnvironmentVariables["PS2X_GRAVA"] = Path.Combine(diag, "video.mkv");
            var linhas = new List<string> { "inicio=" + DateTime.Now.ToString("s"), "maquina=" + Environment.MachineName, "nucleos=" + Environment.ProcessorCount };
            foreach (var kv in op.Ambiente(raiz)) linhas.Add(kv.Key + "=" + kv.Value);
            try { File.WriteAllLines(Path.Combine(diag, "opcoes.txt"), linhas.ToArray()); } catch (IOException) { }
        }
        return Process.Start(si);
    }

    void Jogar()
    {
        string exe, iso;
        string problema = Confere(out exe, out iso);
        if (problema != null) { Avisa(problema); return; }
        Mudou();
        Grava();
        try
        {
            var p = Abre(exe, iso);
            bJogar.Enabled = false;
            lEstado.Text = "Jogo aberto.";
            WindowState = FormWindowState.Minimized;
            p.Exited += (s, e) => BeginInvoke(new Action(() =>
            {
                bJogar.Enabled = true;
                WindowState = FormWindowState.Normal;
                int codigo = p.ExitCode;
                lEstado.Text = codigo == 0 ? "O jogo foi fechado." : "O jogo fechou com o código " + codigo + ". Veja o registro na aba Ajuda.";
            }));
            p.EnableRaisingEvents = true; // depois de assinar: um jogo que fecha na hora não pode perder o aviso
            if (p.HasExited && bJogar.Enabled == false) { bJogar.Enabled = true; WindowState = FormWindowState.Normal; }
        }
        catch (Exception e) { Avisa("Não consegui abrir o jogo: " + e.Message); }
    }

    void Atualiza()
    {
        string exe, iso;
        string problema = Confere(out exe, out iso);
        lEstado.Text = problema ?? "Pronto para jogar.";
    }

    public int Foto(string arq)
    {
        // fora da área visível: o desenho dos controles só existe com a janela aberta
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-4000, -4000);
        ShowInTaskbar = false;
        Show();
        Application.DoEvents();
        var abas = Controls.OfType<TabControl>().First();
        var saida = new Bitmap(Width * 2, Height * 2);
        using (var g = Graphics.FromImage(saida))
            for (int i = 0; i < 4 && i < abas.TabCount; ++i)
            {
                abas.SelectedIndex = i;
                Application.DoEvents();
                using (var b = new Bitmap(Width, Height))
                {
                    DrawToBitmap(b, new Rectangle(0, 0, Width, Height));
                    g.DrawImage(b, (i % 2) * Width, (i / 2) * Height);
                }
            }
        saida.Save(arq);
        return 0;
    }

    public int JogarSemJanela()
    {
        string exe, iso;
        string problema = Confere(out exe, out iso);
        if (problema != null) { Console.Error.WriteLine(problema); return 1; }
        Abre(exe, iso);
        return 0;
    }
}
