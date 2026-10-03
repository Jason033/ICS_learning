"""Synthetic serial read/timeout exercise. Requires pySerial; no hardware is used."""

import serial


def hex_bytes(value: bytes) -> str:
    return value.hex(' ').upper() or '(empty)'


def main() -> None:
    # Fictional protocol: AA | LEN(CMD+DATA) | CMD | DATA | XOR(LEN,CMD,DATA).
    request = bytes.fromhex('AA 01 10 11')
    with serial.serial_for_url(
        'loop://', baudrate=9600, bytesize=serial.EIGHTBITS,
        parity=serial.PARITY_NONE, stopbits=serial.STOPBITS_ONE,
        timeout=0.2, write_timeout=0.2,
    ) as port:
        sent = port.write(request)
        first = port.read(2)
        second = port.read(2)
        nothing = port.read(1)

    print('sent bytes:', sent, hex_bytes(request))
    print('read 1:', hex_bytes(first))
    print('read 2:', hex_bytes(second))
    print('read after timeout:', hex_bytes(nothing))
    assert sent == len(request)
    assert first + second == request
    assert nothing == b''
    print('This is only a software echo, not a device reply or RS-232 wiring test.')


if __name__ == '__main__':
    main()
