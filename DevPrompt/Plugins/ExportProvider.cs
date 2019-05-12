using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Extra plugin exports that don't use the [Export] attribute.
    /// </summary>
    internal class ExportProvider : ExportDescriptorProvider
    {
        private App app;
        private Dictionary<string, object> emptyMetadata;

        public ExportProvider(App app)
        {
            this.app = app;
            this.emptyMetadata = new Dictionary<string, object>();
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (contract.ContractType == typeof(IApp))
            {
                yield return new ExportDescriptorPromise(contract,
                    nameof(ExportProvider), true,
                    () => Enumerable.Empty<CompositionDependency>(),
                    deps => ExportDescriptor.Create(this.ActivateApp, this.emptyMetadata));
            }
        }

        private object ActivateApp(LifetimeContext context, CompositionOperation operation)
        {
            return this.app;
        }
    }
}
