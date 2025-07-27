import serial
import struct
import time
import argparse
import sys
import logging
from enum import Enum
from tqdm import tqdm

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
    def __init__(self, port):
        self.communication_open = False
        self.logger = logging.getLogger(__name__)
        self.port = serial.Serial(port,
                                    57600,
                                    timeout=1,
                                    parity=serial.PARITY_NONE,
                                    stopbits=1)
        
    def read_page_contents(self, addr):
        addr_high_bytes = int(addr / 0x100)
        command = struct.pack('<BH', CommandIdentifier.READ_PAGE.value, addr_high_bytes)
        self.logger.debug(f"Read page contents command: {fmthex(command)}")
        self.port.write(command)
        rx = self.port.read(256)
        return rx

    def read_page_checksum(self, addr):
        addr_high_bytes = int(addr / 0x100)
        command = struct.pack('<BHH', CommandIdentifier.READ_CHECKSUM.value, addr_high_bytes, addr_high_bytes)
        self.logger.debug(f"Read page checksum command: {fmthex(command)}")
        self.port.write(command)
        rx = self.port.read(2)
        checksum = struct.unpack('<H', rx)
        return checksum[0]
    
    def write_page_contents(self, addr, data: bytes):
        assert len(data) == 0x100, "Wrong data payload length"
        addr_high_bytes = int(addr / 0x100)
        message = bytearray()
        command = struct.pack('<BH', CommandIdentifier.WRITE_PAGE.value, addr_high_bytes)
        message.extend(command)
        message.extend(data)
        self.logger.debug(f"Write page contents command: {fmthex(message)}")
        self.port.write(message)
        status = self.port.read(1)
        if BootloaderResponse.ACK.value in status:
            return True
        return False
    
    def clear_status(self) -> bool:
        self.port.write(bytes([CommandIdentifier.CLEAR_STATUS.value]))
        rx = self.port.read(1)
        if BootloaderResponse.ACK.value in rx:
            self.logger.debug(f"Clear status OK!")
            return True
        else:
            self.logger.error(f"Clear status failed.")
            return False
    
    def get_status(self) -> bytes:
        self.port.write(bytes([CommandIdentifier.GET_STATUS.value]))
        rx = self.port.read(2)
        self.logger.debug(f"GET_STATUS returned: {fmthex(rx)}")
        return rx
    
    def get_version(self) -> str:
        self.port.reset_input_buffer()
        self.port.write(bytes([CommandIdentifier.GET_VERSION.value]))
        time.sleep(0.1)
        rx_queue_len = self.port.in_waiting
        if rx_queue_len > 0:
            return self.port.read(rx_queue_len)
        return 'N/A'
    
    def init(self):
        self.port.reset_input_buffer()
        # init starts by sending 18 null bytes to sync baudrate
        for i in range(18):
            self.port.write(b'\x00')
            time.sleep(0.04)
            rx_queue_len = self.port.in_waiting
            if rx_queue_len > 0:
                break
        
        rx = self.port.read(rx_queue_len)
        if BootloaderResponse.ACK.value in rx:
            self.logger.debug("ACK response to handshake")
            self.communication_open = True
            return True
        self.logger.error("No response from ECU")
        return False
    
    def unlock(self):
        status = self.get_status()
        if 0x8C in status:
            self.logger.debug("ECU already unlocked.")
            return True
        self.port.write(unlock_command)
        time.sleep(0.1)
        rx_queue_len = self.port.in_waiting
        if rx_queue_len == 0:
            self.logger.error("No response to unlock command.")
            return False
        rx = self.port.read(rx_queue_len)
        if 0x8C in rx:
            self.logger.debug("Unlock OK!")
            return True
        self.logger.error("Unlock command not accepted by ECU.")
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
        self.logger.info("Writing ECU memory...")
        assert len(data) == 0x100000, "ECU image wrong size"
        status = self.get_status()
        if not 0x8C in status:
            self.logger.error("Bootloader status incorrect")
            return False
        self.logger.debug("Status OK")
        if not self.clear_status():
            self.logger.error("Clear bootloader status fail")
            return False
        self.logger.info("Performing full erase...")
        if not self.erase_all():
            self.logger.error("Erase fail")
            return False
        self.logger.debug("Erase OK")

        page_size = 256
        page_count_per_block = 256
        block_count = 16
        total_page_count = block_count * page_count_per_block

        for page in tqdm(range(total_page_count), desc="Writing flash", unit="page"):
            address = page * page_size
            
            for i in range(10): # max retries
                status = self.write_page_contents(address, data[address:address+256])
                if status is True:
                    break
                else:
                    if i >= 9:
                        self.logger.error(f"Error in writing page {page} at addr {address}")
                        self.logger.error("Write aborted, reset ECU and try again.")
                        return False
                    self.logger.warning("Write fail, trying again.")
        return True

    def read_ecu(self):
        self.logger.info("Reading ECU memory...")
        read_contents = bytearray() # complete flash contents here
        page_size = 256
        page_count_per_block = 256
        block_count = 16
        total_page_count = block_count * page_count_per_block

        for page in tqdm(range(total_page_count), desc="Reading flash", unit="page"):
            address = page * page_size
            
            for i in range(10): # max retries
                checksum = self.read_page_checksum(address)
                contents = self.read_page_contents(address)
                sum_iter = struct.iter_unpack('>H', contents)
                expected_checksum = sum([i[0] for i in sum_iter]) % 65536
                if checksum == expected_checksum:
                    self.logger.debug(f"Read page from address {address} OK")
                    read_contents.extend(contents)
                    break
                else:
                    if i >= 9:
                        self.logger.error("Read aborted, reset ECU and try again.")
                        return []
                    self.logger.warning(f"Read fail (checksum mismatch {expected_checksum} and {checksum}), trying again.")
        return read_contents

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="M32R factory bootloader communication tool to read or write the ECU flash contents"
    )
    parser.add_argument(
        "mode",
        choices=["read", "write"],
        help="Mode of operation: 'read' to read ECU contents to a file, 'write' to flash ECU from a file."
    )
    parser.add_argument(
        "file_path",
        help="Path to the file. In 'read' mode, ECU contents will be saved to this file. In 'write' mode, ECU will be flashed with this file's contents."
    )
    parser.add_argument(
        "-p", "--port",
        help="Port name of the UART adapter, e.g. /dev/tty... in Unix systems",
        required=True
    )
    parser.add_argument(
        "-v", "--verbose",
        action="store_true",
        help="Enable verbose logging output"
    )
    
    args = parser.parse_args()
    
    # Configure logging
    log_level = logging.DEBUG if args.verbose else logging.INFO
    logging.basicConfig(
        level=log_level,
        format='%(levelname)s: %(message)s'
    )
    logger = logging.getLogger(__name__)
    
    comm = BootloaderComm(args.port)
    if not comm.init():
        logger.error("Communication init failed, aborting.")
        sys.exit(1)
    if not comm.unlock():
        logger.error("Bootloader unlock failed, aborting.")
        sys.exit(1)
    logger.debug("Communication with ECU initialized.")
    if args.mode == "read":
        contents = comm.read_ecu()
        if len(contents) > 0:
            with open(args.file_path, "wb") as f:
                f.write(contents)
            logger.info("Read successful.")
        else:
            logger.error("Read failed.")
        
    elif args.mode == "write":
        with open(args.file_path, "rb") as f:
            data = f.read()
        if comm.write_ecu(data):
            logger.info("Write successful.")
        else:
            logger.error("Write failed.")
    else:
        logger.error(f"Unknown mode: {args.mode}")
        sys.exit(1)