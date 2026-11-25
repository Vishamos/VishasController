namespace VishasController;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 트레이 아이콘이 두 개 생기지 않도록 한 번만 실행
        using var mutex = new Mutex(true, @"Local\VishasController", out bool first);
        if (!first)
        {
            MessageBox.Show("VishasController가 이미 실행 중입니다. 작업 표시줄 오른쪽 트레이를 확인하세요.",
                "VishasController", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
