using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using OmmCMS.Admin.Controllers;
using OmmCMS.Admin.Models.Blogs;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Content.Media.Storage;
using OmmCMS.Engine.Modularity;
using OmmCMS.Events;
using OmmCMS.Json;
using OmmCMS.Web.Modelling;

namespace OmmCMS.Web.Tests.Admin;

[TestFixture]
public class MediaUploadRegressionTests
{
    [TestCase(nameof(BlogEditModel.PreviewImageId))]
    [TestCase(nameof(BlogEditModel.CoverImageId))]
    public void BlogEditModel_media_properties_define_content_album_metadata(string propertyName)
    {
        var property = typeof(BlogEditModel).GetProperty(propertyName);
        Assert.That(property, Is.Not.Null);

        var uiHint = property.GetCustomAttribute<UIHintAttribute>();
        Assert.That(uiHint?.UIHint, Is.EqualTo("Media"));

        var metadata = property
            .GetCustomAttributes<AdditionalMetadataAttribute>()
            .ToDictionary(x => x.Name, x => x.Value);

        Assert.That(metadata.GetValueOrDefault("album"), Is.EqualTo(SystemAlbumProvider.Content));
        Assert.That(metadata.GetValueOrDefault("transientUpload"), Is.EqualTo(true));
        Assert.That(metadata.GetValueOrDefault("entityType"), Is.EqualTo("BlogPost"));
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task MediaController_upload_defaults_missing_path_to_files_album(string path)
    {
        string savedPath = null;
        var mediaService = new Mock<IMediaService>();
        var urlGenerator = new Mock<IMediaUrlGenerator>();
        urlGenerator
            .Setup(x => x.GenerateUrl(It.IsAny<MediaFileInfo>(), It.IsAny<QueryString>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns("/media/42/file/photo.png");

        mediaService
            .SetupGet(x => x.StorageProvider)
            .Returns(Mock.Of<IMediaStorageProvider>());
        mediaService
            .Setup(x => x.CombinePaths(It.IsAny<string[]>()))
            .Returns((string[] parts) => string.Join('/', parts.Where(p => !string.IsNullOrEmpty(p))));
        mediaService
            .Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<DuplicateFileHandling>()))
            .Callback<string, Stream, bool, DuplicateFileHandling>((p, _, _, _) => savedPath = p)
            .ReturnsAsync(new MediaFileInfo(
                new MediaFile
                {
                    Id = 42,
                    Name = "photo.png",
                    Extension = "png",
                    MimeType = "image/png",
                    MediaType = "image",
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                },
                mediaService.Object,
                urlGenerator.Object,
                SystemAlbumProvider.Files));

        var typeResolver = new Mock<IMediaTypeResolver>();
        typeResolver
            .Setup(x => x.GetExtensionMediaTypeMap())
            .Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["png"] = "image" });

        var jsonOptions = new JsonOptions();
        SmartJsonOptions.Default.ApplyTo(jsonOptions.JsonSerializerOptions);

        var eventPublisher = new Mock<IEventPublisher>();
        var providerManager = new Mock<IProviderManager>();
        var controller = new MediaController(
            null,
            mediaService.Object,
            typeResolver.Object,
            new MediaExceptionFactory(),
            eventPublisher.Object,
            new Lazy<IMediaMover>(() => null),
            new Lazy<ModuleManager>(() => null),
            providerManager.Object,
            Options.Create(jsonOptions));

        var bytes = new byte[] { 137, 80, 78, 71 };
        var formFile = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", @"C:\fakepath\photo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "multipart/form-data";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { formFile });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.Upload(path);

        Assert.That(actionResult, Is.InstanceOf<JsonResult>());
        Assert.That(savedPath, Is.EqualTo("file/photo.png"));
    }

    [Test]
    public void RoxyFileManager_parses_conf_json_with_booleans_and_rejects_forbidden_extensions()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OmmCMS.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "Could not locate repository root.");
        var webRootPath = Path.Combine(dir.FullName, "src", "OmmCMS.Web", "wwwroot");
        Assert.That(File.Exists(Path.Combine(webRootPath, "lib", "roxyfm", "conf.json")), Is.True);

        var appContext = new Mock<OmmCMS.Engine.IApplicationContext>();
        appContext.SetupGet(x => x.WebRoot).Returns(new OmmCMS.IO.LocalFileSystem(webRootPath));

        var albumRegistry = new Mock<IAlbumRegistry>();
        albumRegistry
            .Setup(x => x.GetAlbumByName(SystemAlbumProvider.Files))
            .Returns(new AlbumInfo { Name = SystemAlbumProvider.Files });

        var controller = new RoxyFileManagerController(
            appContext.Object,
            Mock.Of<IMediaService>(),
            Mock.Of<IFolderService>(),
            albumRegistry.Object,
            Mock.Of<IMediaTypeResolver>(),
            Mock.Of<IMediaStorageConfiguration>(),
            Mock.Of<OmmCMS.Core.Localization.ILocalizationFileResolver>());

        var isAllowed = typeof(RoxyFileManagerController).GetMethod("IsAllowedFileType", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(isAllowed, Is.Not.Null);

        Assert.That((bool)isAllowed.Invoke(controller, [".aspx"]), Is.False);
        Assert.That((bool)isAllowed.Invoke(controller, [".exe"]), Is.False);
        Assert.That((bool)isAllowed.Invoke(controller, [".PHP"]), Is.False);
        Assert.That((bool)isAllowed.Invoke(controller, [".png"]), Is.True);
        Assert.That((bool)isAllowed.Invoke(controller, [".jpg"]), Is.True);
    }
}
