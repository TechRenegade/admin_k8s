using k8s;
using k8s.Models;

namespace admin_k8s
{
    public class WorkNodes
    {
        /// <summary>
        /// Метод для просмотра информации обо всех узлах
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task GetAllNodes(IKubernetes client)
        {
            try
            {
                var nodeList = await client.CoreV1.ListNodeAsync();
                Console.WriteLine("Список всех узлов:");

                foreach (var node in nodeList.Items)
                {
                    Console.WriteLine($"Имя узла: {node.Metadata.Name}");
                    Console.WriteLine($"Статус узла: {node.Status.Conditions[0].Status}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении списка узлов: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для добавления нового узла
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task AddNode(IKubernetes client)
        {
            Console.WriteLine("Введите имя узла:");
            string? nodeName = Console.ReadLine();

            try
            {
                var newNode = new V1Node
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = nodeName
                    }
                };

                var createdNode = await client.CoreV1.CreateNodeAsync(newNode);
                Console.WriteLine($"Узел {createdNode.Metadata.Name} успешно добавлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении узла: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для удаления узла
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task RemoveNode(IKubernetes client)
        {
            Console.WriteLine("Введите имя узла для удаления:");
            string? nodeName = Console.ReadLine();

            try
            {
                await client.CoreV1.DeleteNodeAsync(nodeName, new V1DeleteOptions());
                Console.WriteLine($"Узел {nodeName} успешно удален.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении узла: {ex.Message}");
            }
        }
    }
}