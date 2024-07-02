using k8s;
using k8s.Models;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace admin_k8s
{
    public class WorkPods
    {
        /// <summary>
        /// Метод для создания пода с докер образом
        /// </summary>
        /// <param name="client"></param>
        /// <param name="portMappings"></param>
        /// <returns></returns>
        public static async Task CreatePod(IKubernetes client, Dictionary<int, int> portMappings)
        {
            Console.WriteLine("Введи имя пода:");
            string? podName = Console.ReadLine();
            Console.WriteLine("Введи имя образа:");
            string? imageName = Console.ReadLine();

            try
            {
                if (!await CheckDockerImageExists(imageName))
                {
                    Console.WriteLine($"Образ Docker с именем {imageName} не найден.");
                    return;
                }

                V1Pod pod = new V1Pod()
                {
                    Metadata = new V1ObjectMeta()
                    {
                        Name = podName
                    },
                    Spec = new V1PodSpec()
                    {
                        Containers = new List<V1Container>()
                        {
                            new V1Container()
                            {
                                Name = "container",
                                Image = imageName,
                                Ports = new List<V1ContainerPort>()
                            }
                        }
                    }
                };

                foreach (var mapping in portMappings)
                {
                    pod.Spec.Containers[0].Ports.Add(new V1ContainerPort(mapping.Value, name: $"port-{mapping.Key}"));
                }

                await client.CoreV1.CreateNamespacedPodAsync(pod, "default");

                Console.WriteLine($"Под {podName} создан.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании пода {podName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для проброса портов
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task PortForward(IKubernetes client)
        {
            Console.WriteLine("Введи имя пода:");
            string? podName = Console.ReadLine();
            Console.WriteLine("Введи локальный порт:");
            int localPort = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Введи порт контейнера:");
            int remotePort = Convert.ToInt32(Console.ReadLine());

            try
            {
                await Forward(client, podName, localPort, remotePort);

                Console.WriteLine($"Порт {remotePort} пода {podName} проброшен на локальный порт {localPort}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при пробросе портов: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для выполнения команды в поде
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task ExecuteCommandInPod(IKubernetes client)
        {
            Console.WriteLine("Введи имя пода:");
            string? podName = Console.ReadLine();
            string containerName = "server-img";
            
            Console.WriteLine("Введи команду:");
            string? command = Console.ReadLine();

            try
            {
                var webSocket =
                await client.WebSocketNamespacedPodExecAsync(podName, "default", command, containerName).ConfigureAwait(false);

                var demux = new StreamDemuxer(webSocket);
                demux.Start();

                var buff = new byte[4096];
                var stream = demux.GetStream(1, 1);
                var read = stream.Read(buff, 0, 4096);
                var str = Encoding.Default.GetString(buff);

                Console.WriteLine($"Результат выполнения команды в поде {podName}:");
                Console.WriteLine(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении команды в поде {podName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для получения информации о всех подах
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task GetPodsInfo(IKubernetes client)
        {
            try
            {
                var podsList = await client.CoreV1.ListNamespacedPodAsync("default");

                Console.WriteLine("Статистика:");

                foreach (var pod in podsList.Items)
                {
                    Console.WriteLine($"Имя пода: {pod.Metadata.Name}");
                    Console.WriteLine($"Создан: {pod.Metadata.CreationTimestamp}");
                    Console.WriteLine($"Статус: {pod.Status.Phase}");
                    Console.WriteLine($"IP-адрес: {pod.Status.PodIP}");
                    Console.WriteLine($"Namespace: {pod.Metadata.Namespace}");
                    Console.WriteLine($"UID: {pod.Metadata.Uid}");
                    
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении информации: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для остановки пода
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task StopPod(IKubernetes client)
        {
            Console.WriteLine("Введи имя пода для остановки:");
            string? podName = Console.ReadLine();

            try
            {
                var patch = new V1Patch("{\"spec\":{\"active\":false}}");
                await client.CoreV1.PatchNamespacedPodAsync(new V1Patch(patch), podName, "default");

                Console.WriteLine($"Под {podName} остановлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при остановке пода {podName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для удаления пода
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task DeletePod(IKubernetes client)
        {
            Console.WriteLine("Введи имя пода для удаления:");
            string? podName = Console.ReadLine();

            try
            {
                var deleteOptions = new V1DeleteOptions();
                await client.CoreV1.DeleteNamespacedPodAsync(podName, "default", body: deleteOptions);

                Console.WriteLine($"Под {podName} удален.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении пода {podName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для проверки существования докер образа
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public static async Task<bool> CheckDockerImageExists(string imageName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"images -q {imageName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            return !string.IsNullOrWhiteSpace(output);
        }

        /// <summary>
        /// Этот метод устанавливает прямое соединение между локальным портом и удаленным портом внутри пода. 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="podName"></param>
        /// <param name="localPort"></param>
        /// <param name="remotePort"></param>
        /// <returns></returns>
        private static async Task Forward(IKubernetes client, string podName, int localPort, int remotePort)
        {
            var webSocket = await client.WebSocketNamespacedPodPortForwardAsync(podName, "default", new int[] { remotePort }, "v4.channel.k8s.io");
            var demux = new StreamDemuxer(webSocket, StreamType.PortForward);
            demux.Start();

            var stream = demux.GetStream((byte?)0, (byte?)0);

            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, localPort);
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(100);

            Socket? handler = null;

            var accept = Task.Run(() =>
            {
                while (true)
                {
                    handler = listener.Accept();
                    var bytes = new byte[4096];
                    while (true)
                    {
                        int bytesRec = handler.Receive(bytes);
                        stream.Write(bytes, 0, bytesRec);
                        if (bytesRec == 0 || Encoding.ASCII.GetString(bytes, 0, bytesRec).IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }
                }
            });

            var copy = Task.Run(() =>
            {
                var buff = new byte[4096];
                while (true)
                {
                    var read = stream.Read(buff, 0, 4096);
                    if (handler != null)
                    {
                        handler.Send(buff, read, 0);
                    }
                    else
                    {
                        Console.WriteLine("Handler имеет значение null, не удается отправить данные.");
                        break;
                    }
                }
            });

            await accept;
            await copy;

            if (handler != null)
            {
                handler.Close();
            }
            listener.Close();
        }

        /// <summary>
        /// Метод для просмотра вывода пода
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task ReadPodLogs(IKubernetes client)
        {
            Console.WriteLine("Введи имя пода:");
            string? podName = Console.ReadLine();

            try
            {
                var podsList = await client.CoreV1.ListNamespacedPodAsync("default");
                var targetPod = podsList.Items.FirstOrDefault(pod => pod.Metadata.Name == podName);
                if (targetPod != null)
                {
                    string namespaceName = targetPod.Metadata.Namespace();

                    var stream = client.CoreV1.ReadNamespacedPodLog(podName, namespaceName, follow: true);
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        Console.WriteLine(line);
                    }
                }
                else
                {
                    Console.WriteLine($"Под с именем {podName} не найден.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении логов из пода {podName}: {ex.Message}");
            }
        }
    }
}