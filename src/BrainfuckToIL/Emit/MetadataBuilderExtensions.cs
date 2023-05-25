using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BrainfuckToIL.Emit;

internal static class MetadataBuilderExtensions
{
    /// <summary>
    /// Gets a type reference from metadata.
    /// </summary>
    /// <param name="metadata">The metadata builder.</param>
    /// <param name="assembly">The assembly to get the type reference from.</param>
    /// <param name="namespace">The namespace of the type.</param>
    /// <param name="name">The name of the type.</param>
    public static TypeReferenceHandle GetType(
        this MetadataBuilder metadata,
        AssemblyReferenceHandle assembly,
        string @namespace,
        string name) =>
        metadata.AddTypeReference(
            assembly,
            metadata.GetOrAddString(@namespace),
            metadata.GetOrAddString(name));

    /// <summary>
    /// Creates a method signature.
    /// </summary>
    /// <param name="metadata">The metadata builder.</param>
    /// <param name="isInstanceMethod">Whether the method is an instance method or a static method.</param>
    /// <param name="signature">An action to configure the signature of the method.</param>
    public static BlobHandle CreateSignature(
        this MetadataBuilder metadata,
        bool isInstanceMethod,
        Action<MethodSignatureEncoder> signature)
    {
        var signatureBuilder = new BlobBuilder();
        var signatureEncoder = new BlobEncoder(signatureBuilder)
            .MethodSignature(isInstanceMethod: isInstanceMethod);
        signature(signatureEncoder);
        var signatureBlob = metadata.GetOrAddBlob(signatureBuilder);

        return signatureBlob;
    }
    
    /// <summary>
    /// Gets a method signature and handle from metadata. 
    /// </summary>
    /// <param name="metadata">The metadata builder.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="containingType">The containing type of the method.</param>
    /// <param name="isInstanceMethod">Whether the method is an instance method or a static method.</param>
    /// <param name="signature">An action to configure the signature of the method.</param>
    public static (BlobHandle signature, MemberReferenceHandle member) GetMethod(
        this MetadataBuilder metadata,
        string name,
        TypeReferenceHandle containingType,
        bool isInstanceMethod,
        Action<MethodSignatureEncoder> signature
    )
    {
        var signatureBlob = metadata.CreateSignature(isInstanceMethod, signature);

        var method = metadata.AddMemberReference(
            containingType,
            metadata.GetOrAddString(name),
            signatureBlob);

        return (signatureBlob, method);
    }
}
