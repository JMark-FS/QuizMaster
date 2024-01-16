"use client";

import { useEffect, useState } from "react";
import webIcon from "/public/web-icon.png";
import androidIcon from "/public/android-icon.png";
import appleIcon from "/public/apple-icon.png";
import editIcon from "/public/edit-icon.svg";
import Image from "next/image";
import { SystemInfoDto } from "@/lib/definitions";
import { fetchSystemInfo } from "@/lib/quizData";
import EditSystemInfoModal from "../modals/EditSystemInfoModal";
import Link from "next/link";

export default function SystemInfoCard({ email }: { email: string }) {
    const [systemInfo, setSystemInfo] = useState<SystemInfoDto>();
    const [openEditInfoModal, setOpenEditInfoModal] = useState<boolean>(false);

    useEffect(() => {
        fetchSystemInfo().then((res) => {
            setSystemInfo(res);
        });
    }, [openEditInfoModal]);

    return (
        <>
            <div className="flex flex-row gap-2">
                <p className=" font-bold text-2xl">About QuizMaster</p>
                {email === "admin@gmail.com" ? (
                    <div
                        className="flex flex-row justify-center items-center w-32 p-2 bg-[#169B47] gap-2 rounded-md hover:bg-[#00E154] hover:cursor-pointer"
                        onClick={() => {
                            setOpenEditInfoModal(true);
                        }}
                    >
                        <Image
                            src={editIcon}
                            alt="Edit Logo"
                            width={20}
                            height={20}
                            priority
                        />
                        <p>Edit info</p>
                    </div>
                ) : null}
            </div>
            <p className="font-thin text-xs">version {systemInfo?.version}</p>
            <p className=" font-thin text-base pt-3 ">
                {systemInfo?.description}
            </p>
            <div className="flex flex-col pt-5 gap-2">
                <p className="font-thin text-xs">Avaliable on:</p>
                <div className="flex flex-row gap-3">
                    <Link
                        href={`${systemInfo?.web_link}`}
                        className=" flex flex-col items-center font-thin text-xs gap-1"
                    >
                        <Image
                            src={webIcon}
                            alt="We Icon"
                            width={40}
                            height={40}
                        />
                        <p>Web</p>
                    </Link>
                    <Link
                        href={`${systemInfo?.mobile_link}`}
                        className=" flex flex-col items-center font-thin text-xs gap-1"
                    >
                        <Image
                            src={androidIcon}
                            alt="We Icon"
                            width={40}
                            height={40}
                        />
                        <p>Android</p>
                    </Link>
                    <Link
                        href={`${systemInfo?.ios_link}`}
                        className=" flex flex-col items-center font-thin text-xs gap-1"
                    >
                        <Image
                            src={appleIcon}
                            alt="Web Icon"
                            width={40}
                            height={40}
                        />
                        <p>Apple</p>
                    </Link>
                </div>
            </div>
            {openEditInfoModal && (
                <EditSystemInfoModal
                    systemInfo={systemInfo}
                    opened={openEditInfoModal}
                    onClose={() => setOpenEditInfoModal(false)}
                />
            )}
        </>
    );
}
