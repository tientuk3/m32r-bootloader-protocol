from m32r_bootloader import *

with open('zr374_stock.bin', "rb") as f:
    data = f.read()

comm = BootloaderComm('zr374_stock.bin')
open = comm.open()
#contents = comm.read_ecu()
status = comm.write_ecu(data)
for i in range(64):
    print(hex(contents[i]))
#comm.f_read_page_checksum(0x10600)
#comm.f_read_page_contents(0x10600)