## Denso M32R flasher / bootloader protocol

This tool is able to read and write (at least) Suzuki M32R based ECUs (~2007 onwards). The tool implements the Renesas Serial Protocol (supported by the factory bootloader of M32R 32196 and similar). To use the protocol, the ECU needs to be in the boot mode (MOD0 pin pulled high and reset applied). For certain models, the unlock code needs to be adjusted.

### Command line usage

Install environment

`python -m venv .venv && source .venv/bin/activate && pip install -r requirements.txt`

Command line use

`python m32r_bootloader.py {read|write} <filename> --port <serial port name> [--verbose]`

| Argument         | Description                                                                 |
|------------------|-----------------------------------------------------------------------------|
| `{read,write}`   | Mode: 'read' to read ECU contents to a file, 'write' to flash ECU from a file.      |
| `filename`     | Path to the file where read contents will be placed (in read mode) or which is flashed to the ECU (in write mode)             |
| `-p, --port`     | Port name of the UART adapter, e.g. /dev/tty... in Unix systems         |
| `-v, --verbose`     | Enable verbose logging output        |

### Run tests

`python -m pytest`
