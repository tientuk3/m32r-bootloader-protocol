import sys
import os
import struct
from pathlib import Path
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../src')))

from m32r_bootloader import BootloaderComm, BootloaderResponse, CommandIdentifier, erase_command

# replaces serial.Serial with a fake bootloader connection
class FakeBootloaderPort():
    def __init__(self):
        with open(Path(__file__).parent / 'example_ecu_binary.bin', 'rb') as f:
            self.binary_data = f.read()
        self.unlocked = False
        self.initialized = False
        self.init_bytes_count = 0
        self.in_waiting = 0
        self.response_to_last_command = None
        self.flash_contents = bytearray(0x100000)
        
    def reset_input_buffer(self):
        self.response_to_last_command = None

    def write(self, data: bytes):
        self.response_to_last_command = self._get_response(data)
        self.in_waiting = len(self.response_to_last_command)
    
    def read(self, size: int) -> bytes:
        response_contents = self.response_to_last_command[:size]
        self.reset_input_buffer()
        self.in_waiting = 0
        return response_contents

    def _get_response(self, command) -> bytes:
        if command is None:
            return bytes([])  # Default if nothing has been requested
        
        if not self.initialized:
            if command == bytes([0x0]):
                self.init_bytes_count += 1
                if self.init_bytes_count == 18:
                    self.initialized = True
                    return bytes([BootloaderResponse.ACK.value])
            else:
                self.init_bytes_count = 0
        
        if command == erase_command:
            return bytes([BootloaderResponse.ACK.value])
                
        if command[0] == CommandIdentifier.GET_VERSION.value:
            raise ValueError('not implemented')
        
        if command[0] == CommandIdentifier.GET_STATUS.value:
            return bytes([0x80, 0x8C])
        
        if command[0] == CommandIdentifier.CLEAR_STATUS.value:
            return bytes([BootloaderResponse.ACK.value])
        
        if command[0] == CommandIdentifier.ERASE_BLOCK.value:
            return bytes([BootloaderResponse.ACK.value])
        
        if command[0] == CommandIdentifier.READ_PAGE.value:
            command_id, addr_param = struct.unpack('<BH', command)
            address = addr_param << 8
            page_data = self.binary_data[address:address + 256]
            return bytes(page_data)
        
        if command[0] == CommandIdentifier.READ_CHECKSUM.value:
            command_id, addr_param, _ = struct.unpack('<BHH', command)
            address = addr_param << 8
            page_data = self.binary_data[address:address + 256]
            sum_iter = struct.iter_unpack('>H', page_data)
            checksum = sum([i[0] for i in sum_iter]) % 65536
            return struct.pack('>BB', checksum & 0xFF, checksum >> 8)
        
        if command[0] == CommandIdentifier.WRITE_PAGE.value:
            command_id, addr_param = struct.unpack('<BH', command[:3])
            address = addr_param << 8
            self.flash_contents[address:address+256] = command[3:]
            return bytes([BootloaderResponse.ACK.value])
        
        # If unknown command
        return bytes([])

def test_read_ecu(mocker):
    mocker.patch('m32r_bootloader.serial.Serial', return_value=FakeBootloaderPort())
    
    comm = BootloaderComm("dummy_port")
    comm.init()
    comm.unlock()
    contents = comm.read_ecu()
    
    with open(Path(__file__).parent / 'example_ecu_binary.bin', 'rb') as f:
        expected_contents = f.read()
        
    assert contents == expected_contents
    
def test_write_ecu(mocker):
    mocker.patch('m32r_bootloader.serial.Serial', return_value=FakeBootloaderPort())
    
    comm = BootloaderComm("dummy_port")
    comm.init()
    comm.unlock()
    
    with open(Path(__file__).parent / 'example_ecu_binary.bin', 'rb') as f:
        data = f.read()
    
    assert comm.write_ecu(data)
    assert comm.port.flash_contents == data
    