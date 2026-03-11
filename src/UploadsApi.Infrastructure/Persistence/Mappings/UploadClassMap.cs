using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using UploadsApi.Domain.Entities;
using UploadsApi.Domain.Enums;

namespace UploadsApi.Infrastructure.Persistence.Mappings;

[ExcludeFromCodeCoverage]
public static class UploadClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Upload)))
            return;

        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("CamelCase", conventionPack, _ => true);

        BsonClassMap.RegisterClassMap<Upload>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(x => x.Id)
                .SetIdGenerator(ObjectIdGenerator.Instance)
                .SetSerializer(new ObjectIdSerializer());

            cm.MapMember(x => x.Status)
                .SetSerializer(new EnumSerializer<UploadStatus>(BsonType.String));

            // Intentional: MongoDB requires access to the private parameterless constructor for deserialization.
            // The domain entity uses a private constructor to enforce creation via the static factory method.
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var constructor = typeof(Upload).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
#pragma warning restore S3011

            if (constructor != null)
            {
                cm.MapConstructor(constructor);
            }
        });
    }
}
