#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class LR35902;
	class Timer;
	class PPU;
	class GBAudio;
	class SerialPort;
	class Mapper;
	
	class MemoryManager
	{
	public:
				
		MemoryManager()
		{

		};

		uint8_t ReadMemory(uint32_t addr);
		uint8_t PeekMemory(uint32_t addr);
		void WriteMemory(uint32_t addr, uint8_t value);
		uint8_t Read_Registers(uint32_t addr);
		void Write_Registers(uint32_t addr, uint8_t value);
		
		#pragma region Declarations

		PPU* ppu_pntr = nullptr;
		GBAudio* psg_pntr = nullptr;
		LR35902* cpu_pntr = nullptr;
		Timer* timer_pntr = nullptr;
		SerialPort* serialport_pntr = nullptr;
		Mapper* mapper_pntr = nullptr;
		uint8_t* ROM = nullptr;
		uint8_t* Cart_RAM = nullptr;
		uint8_t* bios_rom = nullptr;

		// initialized by core loading, not savestated
		uint32_t ROM_Length;
		uint32_t ROM_Mapper;
		uint32_t Cart_RAM_Length;

		// controls are not stated
		uint8_t controller_byte_1, controller_byte_2;
		uint8_t* kb_rows;

		// State
		bool PortDEEnabled = false;
		bool lagged;
		bool start_pressed;
		bool is_GBC;
		bool GBC_compat;
		bool speed_switch, double_speed;
		bool in_vblank;
		bool GB_bios_register;
		bool HDMA_transfer;
		bool _islag;
		
		uint8_t REG_FFFF, REG_FF0F, REG_FF0F_OLD;
		uint8_t _scanlineCallbackLine;
		uint8_t input_register;
		uint32_t RAM_Bank;
		uint32_t VRAM_Bank;

		uint8_t IR_reg, IR_mask, IR_signal, IR_receive, IR_self;
		uint32_t IR_write;
		uint32_t addr_access;
		uint32_t Acc_X_state;
		uint32_t Acc_Y_state;

		// several undocumented GBC Registers
		uint8_t undoc_6C, undoc_72, undoc_73, undoc_74, undoc_75, undoc_76, undoc_77;
		uint8_t controller_state;

		uint8_t ZP_RAM[0x80] = {};
		uint8_t RAM[0x8000] = {};
		uint8_t VRAM[0x10000] = {};
		uint8_t OAM[0x10000] = {};
		uint8_t cart_ram[0x8000] = {};
		uint8_t unmapped[0x400] = {};
		uint32_t _vidbuffer[160 * 144] = {};
		uint32_t color_palette[4] = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };

		uint32_t FrameBuffer[160 * 144] = {};

		#pragma endregion

		#pragma region Functions

		// NOTE: only called when checks pass that the files are correct
		void Load_BIOS(uint8_t* bios, bool GBC_console)
		{
			if (GBC_console)
			{
				bios_rom = new uint8_t[2304];
				memcpy(bios_rom, bios, 2304);
			}
			else
			{
				bios_rom = new uint8_t[256];
				memcpy(bios_rom, bios, 256);
			}
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			ROM = new uint8_t[ext_rom_size_1];

			memcpy(ROM, ext_rom_1, ext_rom_size_1);

			ROM_Length = ext_rom_size_1;
			ROM_Mapper = ext_rom_mapper_1;
		}

		// Switch Speed (GBC only)
		uint32_t SpeedFunc(uint32_t temp)
		{
			if (is_GBC)
			{
				if (speed_switch)
				{
					speed_switch = false;
					uint32_t ret = double_speed ? 70224 * 2 : 70224 * 2; // actual time needs checking
					double_speed = !double_speed;
					return ret;
				}

				// if we are not switching speed, return 0
				return 0;
			}

			// if we are in GB mode, return 0 indicating not switching speed
			return 0;
		}

		void Register_Reset()
		{
			input_register = 0xCF; // not reading any input

			REG_FFFF = 0;
			REG_FF0F = 0xE0;
			REG_FF0F_OLD = 0xE0;

			//undocumented registers
			undoc_6C = 0xFE;
			undoc_72 = 0;
			undoc_73 = 0;
			undoc_74 = 0;
			undoc_75 = 0x8F;
			undoc_76 = 0;
			undoc_77 = 0;
		}

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(PortDEEnabled ? 1 : 0); saver++;
			*saver = (uint8_t)(lagged ? 1 : 0); saver++;
			*saver = (uint8_t)(start_pressed ? 1 : 0); saver++;

			std::memcpy(saver, &RAM, 0x10000); saver += 0x10000;
			std::memcpy(saver, &cart_ram, 0x8000); saver += 0x8000;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			PortDEEnabled = *loader == 1; loader++;
			lagged = *loader == 1; loader++;
			start_pressed = *loader == 1; loader++;

			std::memcpy(&RAM, loader, 0x10000); loader += 0x10000;
			std::memcpy(&cart_ram, loader, 0x8000); loader += 0x8000;

			return loader;
		}

		#pragma endregion
	};
}