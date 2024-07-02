using k8s;

namespace admin_k8s
{
    internal class Program
    {
        static async Task Menu(IKubernetes client)
        {
            Console.WriteLine("Выбери действие:");
            Console.WriteLine("1 - Создать под c контейнером.");
            Console.WriteLine("2 - Выполнить команду в поде.");
            Console.WriteLine("3 - Посмотреть статы.");
            Console.WriteLine("4 - Остановить под.");
            Console.WriteLine("5 - Удалить под.");
            Console.WriteLine("6 - Пробросить порты.");
            Console.WriteLine("7 - Посмотреть ноды.");
            Console.WriteLine("8 - Добавить ноду.");
            Console.WriteLine("9 - Удалить ноду.");
            Console.WriteLine("10 - Посмотреть вывод.");

            int choice = Convert.ToInt32(Console.ReadLine());

            switch(choice)
            {
            case 1:
                await WorkPods.CreatePod(client, new Dictionary<int, int> { { 8080, 8080 } });
                break;
            case 2:
                await WorkPods.ExecuteCommandInPod(client);
                break;
            case 3:
                await WorkPods.GetPodsInfo(client);
                break;
            case 4:
                await WorkPods.StopPod(client);
                break;
            case 5:
                await WorkPods.DeletePod(client);
                break;
            case 6:
                await WorkPods.PortForward(client);
                break;
            case 7:
                await WorkNodes.GetAllNodes(client);
                break;
            case 8:
                await WorkNodes.AddNode(client);
                break;
            case 9:
                await WorkNodes.RemoveNode(client);
                break;
            case 10:
                await WorkPods.ReadPodLogs(client);
                break;
            default:
                break;
            }
        }

        static async Task Main(string[] args)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            IKubernetes client = new Kubernetes(config);

            while(true)
            {
                await Menu(client);
            }
        }
    }
}