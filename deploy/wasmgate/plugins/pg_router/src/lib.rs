//! `pg_router` — route PostgreSQL connections by database name.
//!
//! It parses the first PostgreSQL StartupMessage:
//!
//!   int32 length | int32 protocol 196608 | key\0value\0 ... \0
//!
//! and returns a `route_pools` index:
//!   2 -> AuthDb
//!   3 -> TaskDb
//!   4 -> NotificationsDb
//!   1 -> deny (unknown database, SSLRequest/GSSENCRequest, or incomplete packet)
//!
//! SSLRequest/GSSENCRequest cannot be routed by database name because the
//! database is not known until after TLS. Configure clients with
//! `SSL Mode=Disable` (Npgsql) / `sslmode=disable` (libpq).

#![no_std]

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_: &PanicInfo) -> ! {
    core::arch::wasm32::unreachable()
}

static mut SCRATCH: [u8; 32 * 1024] = [0; 32 * 1024];

#[no_mangle]
pub extern "C" fn on_data_ptr() -> i32 {
    core::ptr::addr_of_mut!(SCRATCH) as i32
}

#[no_mangle]
pub extern "C" fn on_data(ptr: i32, len: i32) -> i32 {
    if ptr < 0 || len < 8 {
        return 1;
    }

    let payload = unsafe { core::slice::from_raw_parts(ptr as usize as *const u8, len as usize) };
    let Some(total_len) = be32(&payload[0..4]).map(|value| value as usize) else {
        return 1;
    };
    let Some(code) = be32(&payload[4..8]) else {
        return 1;
    };

    // SSLRequest / GSSENCRequest: no database name is available before TLS.
    if code == 80_877_103 || code == 80_877_102 {
        return 1;
    }

    // Protocol version 3.0 (196608).
    if code != 196_608 || total_len < 8 || total_len > payload.len() {
        return 1;
    }

    let mut position = 8;
    let mut database: Option<&[u8]> = None;

    while position < total_len {
        let key_start = position;
        while position < total_len && payload[position] != 0 {
            position += 1;
        }
        if position >= total_len {
            return 1;
        }
        let key = &payload[key_start..position];
        position += 1;

        if key.is_empty() {
            break;
        }

        let value_start = position;
        while position < total_len && payload[position] != 0 {
            position += 1;
        }
        if position >= total_len {
            return 1;
        }
        let value = &payload[value_start..position];
        position += 1;

        if key == b"database" {
            database = Some(value);
        }
    }

    match database {
        Some(value) if value == b"AuthDb" => 2,
        Some(value) if value == b"TaskDb" => 3,
        Some(value) if value == b"NotificationsDb" => 4,
        _ => 1,
    }
}

fn be32(bytes: &[u8]) -> Option<u32> {
    Some(
        ((*bytes.first()? as u32) << 24)
            | ((*bytes.get(1)? as u32) << 16)
            | ((*bytes.get(2)? as u32) << 8)
            | (*bytes.get(3)? as u32),
    )
}
