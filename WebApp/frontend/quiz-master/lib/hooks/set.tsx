import { QuestionSetDTO } from "../definitions";

export async function postQuestionSet({
    questionSet
}: {
    questionSet: QuestionSetDTO;
}) {
    try {
        // Post Question
        const res = await fetch(
            `${process.env.QUIZMASTER_GATEWAY}/gateway/api/set/create`,
            {
                method: "POST",
                mode: "cors",
                body: JSON.stringify(questionSet),
                headers: {
                    "Content-Type": "application/json",
                },
            }
        );

        if (res.status === 200) {
            return res;
        } else {
            throw new Error("Failed to create question");
        }
    } catch (error) {
        throw new Error("Failed to create question.");
    }
}