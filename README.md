# wcg
SOAP Webservice Code Generator

Like `wsdl.exe` and `xsd.exe` merged together but _better_.

## Usage

```cmd
wcg.exe -xsd="C:\your_path\directory_containing_wsdls_and_xsds" -out="C:\other_path\output_directory" -i -v -debug
```

## Command Line Args

All args can be prefixed with "-", "--", or "/" and may be followed by a space " ", colon ":", or equals "=" before the value.  Boolean arguments do not require a value.  String arguments require quotes only if they contain spaces.

* `/wsdl:"dir"` - The directory containing .wsdl files _(default: xsd dir or the cwd if neither present)_
* `/xsd:"dir"` - The directory containing .xsd files _(default: wsdl dir or the cwd if neither present)_
* `/out:"dir"` | `/o:"dir"` - The directory where generated code files will be placed. If not empty, the operation will be cancelled unless interactive is enabled then it will prompt.
* `/namespace` | `/ns` - The namespace to use for all generated code files. _(default: 'Wcg.Generated')_
* `/interactive` | `/i` - Interactive prompts regarding file overwriting and program dismissal. _(default: false)_
* `/recursive` | `/r` - Search subdirectories of the 'wsdl' and 'xsd' paths for files. _(default: false)_
* `/verbose` | `/v` - Increased verbosity of console output. _(default: false)_
* `/debug` | `/dbg` - Display program stack trace information for bug reporting. _(default: false)_
* `/help` | `/?` - Display a help page.

## Features

* **Task API** `async` methods - also exposes synchronous calls. Removes/hides the callback and event -based Async methods.
* **Multiple namespaces** - a base namespace is created and a child namespace for each XSD and WSDL are used. Types are located with `using`s when they reside outside the current file.
* Creates **multiple files** - currently one file per WSDL and XSD.
* **Fast** - as in Console output is the primary bottleneck.
* **Color output** - not even optional! Uses `Console.ForegroundColor` and `Console.BackgroundColor` API so redirection to files isn't cluttered with ANSI escape sequences.

## Wish List

* Fetch files from URLs if not present on disk
* Expose the ability to provide a custom attribute (for those wishing to add logging, etc.)
* Generate a project and solution
* Generate new `{ get; set; }` style properties
* Reduce namespace usings to only the portion that is required
* Reduce Task API and Serialization attribute verbosity via a using statement
* C# style naming of properties with serialization overrides to convert back to the defined casing
* Folder per WSDL and XSD with file per class/enum output
* Drop "specified" fields by using nullable types
* Support for `List<T>` instead of `T[]` types
* Better handling of descriminators (instead of `object` with several `XmlElementAttribute`s, perhaps have ignored properties of the correct type that modify the underlying field.
* Kill all the `<remarks/>`
