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

        public ExportProvider(App app)
        {
            this.app = app;
        }

        /// <summary>
        /// The app and main window cannot be created and owned by System.Composition,
        /// so this method allows them to be exported anyway.
        /// </summary>
        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (contract.ContractType == typeof(Api.IApp))
            {
                yield return ExportProvider.CreateExport(contract, this.ActivateApp, true);
            }
            else if (contract.ContractType == typeof(Api.IAppSettings))
            {
                yield return ExportProvider.CreateExport(contract, this.ActivateAppSettings, true);
            }
        }

        private object ActivateApp(LifetimeContext context, CompositionOperation operation)
        {
            return this.app;
        }

        private object ActivateAppSettings(LifetimeContext context, CompositionOperation operation)
        {
            return this.app.Settings;
        }

        private static ExportDescriptorPromise CreateExport(CompositionContract contract, CompositeActivator activator, bool shared)
        {
            return new ExportDescriptorPromise(contract,
                nameof(ExportProvider), shared,
                () => Enumerable.Empty<CompositionDependency>(),
                deps => ExportDescriptor.Create(activator, new Dictionary<string, object>()));
        }
    }
}
