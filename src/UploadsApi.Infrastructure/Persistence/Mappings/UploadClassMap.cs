using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using UploadsApi.Domain.Entities;
using UploadsApi.Domain.Enums;

namespace UploadsApi.Infrastructure.Persistence.Mappings;

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
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

            cm.MapMember(x => x.Status)
                .SetSerializer(new EnumSerializer<UploadStatus>(BsonType.String));

            var constructor = typeof(Upload).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (constructor != null)
            {
                cm.MapConstructor(constructor);
            }
        });
    }
}
