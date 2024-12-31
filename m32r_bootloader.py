import serial
import struct
import time
from enum import Enum

def fmthex(contents):
    return ' '.join(f'{x:02x}' for x in contents)

class BootloaderResponse(Enum):
    ACK = 0x6
    NAK = 0x15

class CommandIdentifier(Enum):
    GET_VERSION = 0xFB     # get version, responds a string like "VER.1.23"
    GET_STATUS = 0x70      # responds two status bytes SRD1 and SRD2
    CLEAR_STATUS = 0x50    # clear status bytes
    ERASE_BLOCK = 0x20     # erase a block, usually 0x10000 (64k bytes) aligned (refer to hardware manual table 6.6.3)
    READ_PAGE = 0xFF       # read a page (256 bytes) from a 0x100 aligned address
    READ_CHECKSUM = 0xE1
    WRITE_PAGE = 0x41

unlock_command = [0xf5, 0x84, 0x0, 0x0, 0xc, 0x53, 0x55, 0x45, 0x46, 0x49, 0x4d, 0xff, 0xff, 0xff, 0xff, 0x56, 0x30]
erase_command = [0xA7, 0xD0]

class BootloaderComm:
    def __init__(self, fakefile):
        self.communication_open = False
        self.port = serial.Serial('/dev/tty.usbserial-A50285BI',
                                    57600,
                                    timeout=1,
                                    parity=serial.PARITY_NONE,
                                    stopbits=1)
        with open(fakefile, "rb") as f:
            self.flash = f.read()
    
    def f_read_page_contents(self, addr):
        addr_high_bytes = int(addr / 0x100)
        command = struct.pack('<BH', CommandIdentifier.READ_PAGE.value, addr_high_bytes)
        print(f"Read page contents command: {fmthex(command)}")
        return self.flash[addr:addr+256]
    
    def f_read_page_checksum(self, addr):
        addr_high_bytes = int(addr / 0x100)
        command = struct.pack('<BHH', CommandIdentifier.READ_CHECKSUM.value, addr_high_bytes, addr_high_bytes)
        print(f"Read page checksum command: {fmthex(command)}")
        checksum_iter = struct.iter_unpack('>H', self.flash[addr:addr+256])
        checksum = sum([i[0] for i in checksum_iter]) % 65536
        print(f'Checksum {hex(checksum)}')
        return checksum

    def read_page_contents(self, addr):
        addr_high_bytes = addr / 0x100
        command = struct.pack('<BH', CommandIdentifier.READ_PAGE.value, addr_high_bytes)
        print(f"Read page contents command: {fmthex(command)}")
        self.port.write(command)
        rx = self.port.read(256)
        return rx

    def read_page_checksum(self, addr):
        addr_high_bytes = addr / 0x100
        command = struct.pack('<BHH', CommandIdentifier.READ_CHECKSUM.value, addr_high_bytes, addr_high_bytes)
        print(f"Read page checksum command: {fmthex(command)}")
        self.port.write(command)
        rx = self.port.read(2)
        checksum = struct.unpack('<H', rx)
        return checksum
    
    def write_page_contents(self, addr, data: bytes):
        assert len(data) == 0x100, "Wrong data payload length"
        addr_high_bytes = addr / 0x100
        message = bytearray()
        command = struct.pack('<BH', CommandIdentifier.WRITE_PAGE.value, addr_high_bytes)
        message.extend(command)
        message.extend(data)
        print(f"Write page contents command: {fmthex(message)}")
        self.port.write(message)
        status = self.port.read(1)
        if BootloaderResponse.ACK.value in status:
            return True
        return False
    
    def clear_status(self) -> bool:
        self.port.write(bytes([CommandIdentifier.CLEAR_STATUS.value]))
        rx = self.port.read(1)
        if BootloaderResponse.ACK.value in rx:
            return True
        else:
            return False
    
    def get_status(self) -> int:
        self.port.write(bytes([CommandIdentifier.GET_STATUS.value]))
        rx = self.port.read(2)
        return rx
    
    def get_version(self) -> str:
        self.port.reset_input_buffer()
        self.port.write(bytes([CommandIdentifier.GET_VERSION.value]))
        time.sleep(0.1)
        rx_queue_len = self.port.in_waiting
        if rx_queue_len > 0:
            return self.port.read(rx_queue_len)
        return 'N/A'
    
    def open(self):
        self.port.reset_input_buffer()
        for i in range(20):
            self.port.write(b'\x00')
            time.sleep(0.04)
            rx_queue_len = self.port.in_waiting
            if rx_queue_len > 0:
                break
        
        rx = self.port.read(rx_queue_len)
        if BootloaderResponse.ACK.value in rx:
            print("ACK response to handshake")
            self.communication_open = True
            return True
        print("No response from ECU")
        return False
    
    def unlock(self):
        status = self.get_status()
        if 0x8C in status:
            return True
        self.port.write(unlock_command)
        time.sleep(0.1)
        rx_queue_len = self.port.in_waiting
        if rx_queue_len == 0:
            return False
        rx = self.port.read(rx_queue_len)
        if 0x8C in rx:
            return True
        return False
    
    def erase_all(self):
        self.port.reset_input_buffer()
        self.port.write(erase_command)
        for i in range(20):
            time.sleep(1)
            rx_queue_len = self.port.in_waiting
            if rx_queue_len > 0:
                break
        rx = self.port.read(rx_queue_len)
        if BootloaderResponse.ACK.value in rx:
            return True
        return False
    
    def write_ecu(self, data: bytes):
        assert len(data) == 0x100000, "ECU image wrong size"
        status = self.get_status()
        if not BootloaderResponse.ACK.value in status:
            print("Bootloader status incorrect")
            return False
        if not self.clear_status():
            print("Clear bootloader status fail")
            return False
        if not self.erase_all():
            print("Erase fail")
            return False

        page_size = 256
        page_count_per_block = 256
        block_count = 16
        total_page_count = block_count * page_count_per_block

        for page in range(total_page_count):
            address = page * page_size
            status = self.write_page_contents(address, data[address:address+256])
            if status == False:
                print(f"Error in writing page {page} at addr {address}")


    def read_ecu(self):
        read_contents = bytearray() # complete flash contents here
        page_size = 256
        page_count_per_block = 256
        block_count = 16
        total_page_count = block_count * page_count_per_block

        for page in range(total_page_count):
            address = page * page_size
            checksum = self.f_read_page_checksum(address)
            contents = self.f_read_page_contents(address)
            sum_iter = struct.iter_unpack('>H', contents)
            checksum_calc = sum([i[0] for i in sum_iter]) % 65536
            if checksum == checksum_calc:
                print("Read OK")
                read_contents.extend(contents)
            else:
                print("Read fail")
                print(checksum_calc)
                read_contents = []
        return read_contents



