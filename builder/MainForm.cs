using System.Diagnostics;
using System.Text;

namespace MODGDBuilder;

public sealed partial class MainForm : Form
{
    private readonly TextBox log = new();
    private readonly Button build = new();
    private readonly Label status = new();
    private string projectDir = AppContext.BaseDirectory;

    public MainForm()
    {
        Text = "MODGD Builder";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(680, 440);
        StartPosition = FormStartPosition.CenterScreen;

        var title = new Label {
            Text = "MODGD Builder",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 24)
        };
        var subtitle = new Label {
            Text = "Compila tu mod de Geometry Dash con un botón.",
            Font = new Font("Segoe UI", 10),
            AutoSize = true,
            Location = new Point(31, 68)
        };
        build.Text = "COMPILAR MOD";
        build.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        build.Size = new Size(250, 58);
        build.Location = new Point(28, 105);
        build.Click += async (_, _) => await BuildAsync();

        var folder = new Button {
            Text = "Elegir proyecto",
            Size = new Size(160, 40),
            Location = new Point(295, 114)
        };
        folder.Click += (_, _) => ChooseProject();

        status.Text = "Estado: listo";
        status.AutoSize = true;
        status.Location = new Point(475, 127);

        log.Multiline = true;
        log.ReadOnly = true;
        log.ScrollBars = ScrollBars.Vertical;
        log.Font = new Font("Consolas", 9);
        log.Location = new Point(28, 185);
        log.Size = new Size(690, 260);
        log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange([title, subtitle, build, folder, status, log]);
        Log("MODGD Builder iniciado.");
        Log($"Proyecto: {projectDir}");
    }

    private void ChooseProject()
    {
        using var dialog = new FolderBrowserDialog { Description = "Selecciona la carpeta raíz de MODGD" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            projectDir = dialog.SelectedPath;
            Log($"Proyecto seleccionado: {projectDir}");
            status.Text = "Estado: proyecto seleccionado";
        }
    }

    private async Task BuildAsync()
    {
        if (!File.Exists(Path.Combine(projectDir, "mod.json")) ||
            !File.Exists(Path.Combine(projectDir, "CMakeLists.txt")))
        {
            MessageBox.Show("No encontré mod.json y CMakeLists.txt.", "MODGD Builder",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        build.Enabled = false;
        status.Text = "Estado: preparando...";
        log.Clear();

        try
        {
            Log("Comprobando Geode CLI...");
            if (!await RunAsync("geode", "--version"))
            {
                Log("Geode CLI no está instalado. Intentando instalarlo con Winget...");
                if (!await RunAsync("winget", "install --id GeodeSDK.GeodeCLI -e --accept-source-agreements --accept-package-agreements"))
                    throw new Exception("No se pudo instalar Geode CLI automáticamente. Instala Geode CLI y vuelve a abrir el Builder.");
            }

            Log("Comprobando CMake...");
            if (!await RunAsync("cmake", "--version"))
            {
                Log("CMake no está instalado. Intentando instalarlo con Winget...");
                if (!await RunAsync("winget", "install --id Kitware.CMake -e --accept-source-agreements --accept-package-agreements"))
                    throw new Exception("No se pudo instalar CMake automáticamente.");
            }

            Log("Preparando Geode SDK...");
            if (!await RunAsync("geode", "sdk install"))
                throw new Exception("No se pudo instalar el Geode SDK.");

            Log("Comprobando dependencias del mod...");
            await RunAsync("geode", "project check -p windows");

            Log("Compilando MODGD para Windows x64...");
            if (!await RunAsync("geode", "build -p windows --config Release"))
                throw new Exception("La compilación falló. Revisa el registro de arriba.");

            var buildRoots = new[] {
                Path.Combine(projectDir, "build"),
                Path.Combine(projectDir, "build-windows")
            };

            var geode = buildRoots
                .Where(Directory.Exists)
                .SelectMany(p => Directory.EnumerateFiles(p, "*.geode", SearchOption.AllDirectories))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (geode is null)
                throw new Exception("La compilación terminó pero no apareció ningún .geode.");

            var output = Path.Combine(projectDir, "output");
            Directory.CreateDirectory(output);
            var destination = Path.Combine(output, Path.GetFileName(geode));
            File.Copy(geode, destination, true);

            Log($"LISTO: {destination}");
            status.Text = "Estado: COMPILADO";
            Process.Start("explorer.exe", $"/select,\"{destination}\"");
            MessageBox.Show($"Mod compilado correctamente.\n\n{destination}",
                "MODGD Builder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "Estado: ERROR";
            Log("ERROR: " + ex.Message);
            MessageBox.Show(ex.Message, "Error de compilación",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            build.Enabled = true;
        }
    }

    private async Task<bool> RunAsync(string file, string args)
    {
        var psi = new ProcessStartInfo {
            FileName = file,
            Arguments = args,
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) Log(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Log(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private void Log(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(text)); return; }
        log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }
}
