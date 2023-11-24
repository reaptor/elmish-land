module Xml

open System.IO
open System.Text
open System.Xml

type Xml =
    | XmlValue of string
    | XmlElement of string * Xml list
    | XmlAttribute of string * string

    static member elem(name, nodes) = XmlElement(name, nodes)
    static member elem(name, value) = XmlElement(name, [ Xml.value value ])
    static member value value = XmlValue(value)
    static member attr(name, value) = XmlAttribute(name, value)

type Utf8StringWriter() =
    inherit StringWriter()
    let noBomBecauseSwishDoesntLike = false

    let encoding = UTF8Encoding(noBomBecauseSwishDoesntLike)

    override __.Encoding = encoding :> Encoding

let xmlToString xmlns xml =
    let noBomBecauseSwishDoesntLike = false

    let encoding = UTF8Encoding(noBomBecauseSwishDoesntLike)

    let settings =
        XmlWriterSettings(Indent = true, Encoding = encoding, OmitXmlDeclaration = false)

    let sWriter = new Utf8StringWriter()
    let writer = XmlWriter.Create(sWriter, settings)
    writer.WriteStartDocument()

    let rec write =
        function
        | XmlValue(value) -> writer.WriteValue(value)
        | XmlAttribute(name, value) -> writer.WriteAttributeString(name, value)
        | XmlElement(name, nodes) ->
            writer.WriteStartElement(name, xmlns)

            let sorted =
                nodes
                |> List.sortBy (function
                    | XmlAttribute _ -> 1
                    | XmlElement _ -> 2
                    | XmlValue _ -> 3)

            for n in sorted do
                write n

            writer.WriteEndElement()

    write xml
    writer.WriteEndDocument()
    writer.Flush()
    sWriter.ToString()
