using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.Descriptions;
using FubuMVC.Core.Http;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Runtime;
using FubuMVC.Core.Runtime.Conditionals;
using FubuMVC.Core.Runtime.Formatters;
using FubuMVC.Core.View;
using StructureMap.Pipeline;

namespace FubuMVC.Core.Resources.Conneg
{
    public class OutputNode : BehaviorNode, IMayHaveResourceType, DescribesItself, IOutputNode
    {
        private readonly Type _resourceType;
        private readonly IList<IMediaWriter> _media = new List<IMediaWriter>();
        private ConnegSettings _settings;

        private readonly Lazy<IEnumerable<IMediaWriter>> _allMedia; 

        public OutputNode(Type resourceType)
        {
            if (resourceType == typeof (void))
            {
                throw new ArgumentOutOfRangeException("Void is not a valid resource type");
            }

            if (resourceType == null)
            {
                throw new ArgumentNullException("resourceType");
            }

            _resourceType = resourceType;

            _allMedia = new Lazy<IEnumerable<IMediaWriter>>(() => {
                var settings = _settings ?? new ConnegSettings();

                settings.ApplyRules(this);

                return _media;
            });
        }

        public void Add(IFormatter formatter)
        {
            var writer = typeof (FormatterWriter<>).CloseAndBuildAs<object>(formatter, _resourceType).As<IMediaWriter>();
            addWriter(writer);
        }

        private void addWriter(IMediaWriter writer)
        {
            _media.Add(writer);
        }

        public void Add(Type mediaWriterType)
        {
            if (!mediaWriterType.IsOpenGeneric() || !mediaWriterType.Closes(typeof (IMediaWriter<>)) || !mediaWriterType.IsConcreteWithDefaultCtor())
            {
                throw new ArgumentOutOfRangeException("mediaWriterType", "mediaWriterType must implement IMediaWriter<T> and have a default constructor");
            }

            var writerType = mediaWriterType.MakeGenericType(_resourceType);
            

            var writer = Activator.CreateInstance(writerType).As<IMediaWriter>();
            
            addWriter(writer);
        }

        public void Add(IMediaWriter writer)
        {
            var writerType = typeof(IMediaWriter<>).MakeGenericType(_resourceType);
            if (!writerType.IsInstanceOfType(writer))
            {
                throw new ArgumentOutOfRangeException("writer", "writer must implement " + writerType.GetFullName());
            }

            addWriter(writer);
        }



        public IEnumerable<IMediaWriter> Media()
        {
            return _allMedia.Value;
        }

        public IEnumerable<IMediaWriter> Explicits
        {
            get
            {
                return _media;
            }
        } 

        public IEnumerable<IMediaWriter<T>> Media<T>()
        {
            return Media().OfType<IMediaWriter<T>>();
        }

        public override BehaviorCategory Category
        {
            get { return BehaviorCategory.Output; }
        }

        public bool HasView()
        {
            return _media.Any(x => x.Mimetypes.Contains(MimeType.Html.Value));
        }

        public IViewToken DefaultView()
        {
            return
                _media
                    .OfType<IViewWriter>()
                    .Select(x => x.View)
                    .FirstOrDefault();
        }

        public Type ResourceType
        {
            get { return _resourceType; }
        }

        public override string Description
        {
            get { return ToString(); }
        }

        /// <summary>
        /// Use this if you want to override the handling for 
        /// the resource not being found on a chain by chain
        /// basis
        /// </summary>
        public Instance ResourceNotFound { get; set; }

        /// <summary>
        /// Use the specified type T as the resource not found handler strategy
        /// for only this chain
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UseForResourceNotFound<T>() where T : IResourceNotFoundHandler
        {
            ResourceNotFound = new SmartInstance<T>();
        }

        #region DescribesItself Members

        void DescribesItself.Describe(Description description)
        {
            description.Title = "Conneg Output";
            description.ShortDescription = "Render the output for resource " + ResourceType.Name;

            description.AddList("Media", _media);
        }

        #endregion

        #region IMayHaveResourceType Members

        Type IMayHaveResourceType.ResourceType()
        {
            return ResourceType;
        }

        #endregion


        protected override IConfiguredInstance buildInstance()
        {
            var instance = new ConfiguredInstance(typeof (OutputBehavior<>), _resourceType);

            var collection = new ConfiguredInstance(typeof(MediaCollection<>), _resourceType);
            collection.Dependencies.Add<IOutputNode>(this);
            var collectionType = typeof(IMediaCollection<>).MakeGenericType(_resourceType);
            instance.Dependencies.Add(collectionType, collection);

            

            if (ResourceNotFound != null)
            {
                instance.Dependencies.Add(typeof(IResourceNotFoundHandler), ResourceNotFound);
            }

            return instance;
        }


        public void ClearAll()
        {
            _media.Clear();
        }

        public override string ToString()
        {
            return _media.Select(x => x.ToString()).Join(", ");
        }

        public IEnumerable<string> MimeTypes()
        {
            return _allMedia.Value.SelectMany(x => x.Mimetypes).Distinct();
        }

        public bool Writes(MimeType mimeType)
        {
            return MimeTypes().Contains(mimeType.ToString());
        }

        public bool Writes(string mimeType)
        {
            return MimeTypes().Contains(mimeType);
        }

        public void UseSettings(ConnegSettings settings)
        {
            _settings = settings;
        }
    }
}