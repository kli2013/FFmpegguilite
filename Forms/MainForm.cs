public partial class MainForm : Form
{
    private List<TaskInfo> tasks = new List<TaskInfo>();
    private bool isProcessing;
    private CancellationTokenSource cts;
    private SemaphoreSlim hwSemaphore;
    private int maxParallel = 2;
    private int maxHwParallel = 2;

    private async void StartQueue()
    {
        if (isProcessing) return;
        var pending = tasks.Where(t => t.Status == "等待").ToList();
        if (pending.Count == 0) return;

        isProcessing = true;
        cts = new CancellationTokenSource();
        hwSemaphore = new SemaphoreSlim(maxHwParallel);
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = cts.Token };

        try
        {
            await Parallel.ForEachAsync(pending, options, async (task, token) =>
            {
                bool isHw = IsHardwareEncoder(task.Settings.Encoder);
                if (isHw) await hwSemaphore.WaitAsync(token);
                try
                {
                    await ProcessSingleTask(task, token);
                }
                finally
                {
                    if (isHw) hwSemaphore.Release();
                }
            });
        }
        catch (OperationCanceledException) { /* 用户停止 */ }
        finally
        {
            isProcessing = false;
            UpdateTaskListUI();
            AppendInfo("队列处理完成或已停止");
        }
    }

    private async Task ProcessSingleTask(TaskInfo task, CancellationToken token)
    {
        task.Status = "转码中";
        UpdateTaskListUI();
        var runner = new FFmpegProcessRunner();
        var result = await runner.RunAsync(task.Command, token);
        task.Status = result.Success ? "完成" : "失败";
        task.ErrorMsg = result.Error;
        UpdateTaskListUI();
    }
}
