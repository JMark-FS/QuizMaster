import { QUIZMASTER_SET_POST, QUIZMASTER_SET_PUT, QUIZMASTER_SYSTEM_POST_SYSTEM_INFO } from "@/api/api-routes";
import { ContactDetails, ContactUsCreateValues, QuestionSetDTO, SetDTO, SystemInfoDto } from "../definitions";

export async function UpdateSystemInfo({
    systemDetails,
}: {
    systemDetails: SystemInfoDto;
}) {
    try {
        const token = localStorage.getItem("token"); //just temporary
        // Post Question
        const res = await fetch(`${QUIZMASTER_SYSTEM_POST_SYSTEM_INFO}`, {
            method: "POST",
            mode: "cors",
            body: JSON.stringify(systemDetails),
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`,
            },
        });
        console.log(res);
        if (res.status === 200) {
            return res;
        } else {
            throw new Error("Failed to update system info");
        }
    } catch (error) {
        throw new Error("Failed to update question.");
    }
    
}