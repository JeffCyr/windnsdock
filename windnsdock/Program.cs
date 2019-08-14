using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace windnsdock
{
    internal class Program
    {
        private static string HostFilePath = @"C:\Windows\System32\drivers\etc\hosts";

        private static DockerClient Client;

        private static async Task Main(string[] args)
        {
            if (args.Length > 0)
                HostFilePath = args[0];

            Client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                cts.Cancel();
            };

            var semaphore = new SemaphoreSlim(1);
            _ = Task.Run(() => Run(semaphore));

            await Client.System.MonitorEventsAsync(new ContainerEventsParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "type", new Dictionary<string, bool>
                        {
                            { "network", true },
                            { "container", true }
                        }
                    }
                }
            }, new Progress<JSONMessage>(json =>
            {
                semaphore.Release();
            }),
            cts.Token);
        }

        private static async Task Run(SemaphoreSlim semaphore)
        {
            while (true)
            {
                while (semaphore.Wait(0))
                { }

                await Task.Delay(1000);
                
                var containers = await GetContainersInfoAsync();
                UpdateHostFile(containers);

                await semaphore.WaitAsync();
            }
        }

        private static async Task<IEnumerable<(string name, string address)>> GetContainersInfoAsync()
        {
            var containers = await Client.Containers.ListContainersAsync(new ContainersListParameters());

            return containers
                .Select(item => (item.Names.First().Substring(1), item.NetworkSettings.Networks.Values.FirstOrDefault()?.IPAddress))
                .ToList();
        }

        private static void UpdateHostFile(IEnumerable<(string name, string address)> containers)
        {
            const string BeginSectionMarker = "# BeginSection windnsdock";
            const string EndSectionMarker = "# EndSection";

            string content = File.ReadAllText(HostFilePath);
            var lines = new LinkedList<string>(content.Split(new[] { "\r\n" }, StringSplitOptions.None));
            var beginNode = lines.Find(BeginSectionMarker);
            LinkedListNode<string> endNode;

            if (beginNode == null)
            {
                if (lines.Last.Value != "")
                    lines.AddLast(string.Empty);

                beginNode = lines.AddLast(BeginSectionMarker);
                endNode = lines.AddLast(EndSectionMarker);
                lines.AddLast(string.Empty);
            }
            else
            {
                var node = beginNode.Next;

                while (node != null && node.Value != EndSectionMarker)
                {
                    var next = node.Next;
                    lines.Remove(node);
                    node = next;
                }

                endNode = node ?? lines.AddLast(EndSectionMarker);
            }

            foreach (var container in containers)
            {
                string line = $"{container.address} {container.name}";
                endNode.List.AddBefore(endNode, line);
            }

            string newContent = string.Join("\r\n", lines);

            if (newContent != content)
            {
                File.WriteAllText(HostFilePath, newContent);
                Console.WriteLine("host file updated");
            }
        }
    }
}
